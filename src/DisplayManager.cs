using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ScreenControl
{
    /// <summary>
    /// 通过 Windows DisplayConfig API 禁用/恢复显示器。
    /// 禁用时保留主屏幕，禁用所有副屏；再次调用恢复原配置。
    /// </summary>
    internal static class DisplayManager
    {
        // ---------- QueryDisplayConfig / SetDisplayConfig 标志 ----------
        private const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;

        private const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020;
        private const uint SDC_APPLY = 0x00000080;
        private const uint SDC_ALLOW_CHANGES = 0x00000400;

        private const uint DISPLAYCONFIG_PATH_MODE_IDX_INVALID = 0xFFFFFFFF;
        private const uint DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE = 1;

        // ---------- 原生结构体（与 Windows SDK 定义保持一致） ----------

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTL
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_RATIONAL
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_2DREGION
        {
            public uint cx;
            public uint cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
        {
            public ulong pixelRate;
            public DISPLAYCONFIG_RATIONAL hSyncFreq;
            public DISPLAYCONFIG_RATIONAL vSyncFreq;
            public DISPLAYCONFIG_2DREGION activeSize;
            public DISPLAYCONFIG_2DREGION totalSize;
            public uint videoStandard;
            public uint scanLineOrdering;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_SOURCE_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public DISPLAYCONFIG_RATIONAL refreshRate;
            public uint scanLineOrdering;
            public uint targetAvailable;   // BOOL
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_INFO
        {
            public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
            public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_SOURCE_MODE
        {
            public uint width;
            public uint height;
            public DISPLAYCONFIG_RATIONAL pixelRate;
            public POINTL position;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_TARGET_MODE
        {
            public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct DISPLAYCONFIG_MODE_INFO_UNION
        {
            [FieldOffset(0)] public DISPLAYCONFIG_TARGET_MODE targetMode;
            [FieldOffset(0)] public DISPLAYCONFIG_SOURCE_MODE sourceMode;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_MODE_INFO
        {
            public uint infoType;
            public uint id;
            public LUID adapterId;
            public DISPLAYCONFIG_MODE_INFO_UNION modeUnion;
        }

        // ---------- P/Invoke ----------

        [DllImport("user32.dll")]
        private static extern int QueryDisplayConfig(
            uint flags,
            ref uint numPathArrayElements,
            [In, Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
            ref uint numModeInfoArrayElements,
            [In, Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
            out uint currentTopologyId);

        [DllImport("user32.dll")]
        private static extern int SetDisplayConfig(
            uint numPathArrayElements,
            [In] DISPLAYCONFIG_PATH_INFO[] pathArray,
            uint numModeInfoArrayElements,
            [In] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
            uint flags);

        // ---------- 状态 ----------

        private static DISPLAYCONFIG_PATH_INFO[] _savedPaths;
        private static DISPLAYCONFIG_MODE_INFO[] _savedModes;

        /// <summary>当前是否处于"副屏已禁用"状态。</summary>
        public static bool IsSecondaryDisabled => _savedPaths != null;

        /// <summary>
        /// 禁用所有副屏（保留主屏）。成功后返回 true。
        /// 若本机只有一块屏幕，则返回 false 且不做任何更改。
        /// </summary>
        public static bool DisableSecondaryMonitors()
        {
            if (!TryGetActiveConfig(out DISPLAYCONFIG_PATH_INFO[] paths, out DISPLAYCONFIG_MODE_INFO[] modes))
            {
                return false;
            }

            // 主屏的特征：其 source mode 的桌面坐标为 (0,0)
            HashSet<long> primaryKeys = new HashSet<long>();
            foreach (DISPLAYCONFIG_PATH_INFO path in paths)
            {
                DISPLAYCONFIG_MODE_INFO? mode = FindSourceMode(modes, path);
                if (mode.HasValue &&
                    mode.Value.modeUnion.sourceMode.position.x == 0 &&
                    mode.Value.modeUnion.sourceMode.position.y == 0)
                {
                    primaryKeys.Add(SourceKey(path.sourceInfo));
                }
            }

            if (primaryKeys.Count == 0)
            {
                // 无法识别主屏，放弃操作，避免把全部屏幕都禁用掉
                return false;
            }

            bool anyDisabled = false;
            DISPLAYCONFIG_PATH_INFO[] newPaths = (DISPLAYCONFIG_PATH_INFO[])paths.Clone();
            for (int i = 0; i < newPaths.Length; i++)
            {
                if (primaryKeys.Contains(SourceKey(newPaths[i].sourceInfo)))
                {
                    continue; // 保留主屏
                }
                newPaths[i].sourceInfo.modeInfoIdx = DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
                newPaths[i].targetInfo.modeInfoIdx = DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
                anyDisabled = true;
            }

            if (!anyDisabled)
            {
                return false; // 没有副屏
            }

            int ret = SetDisplayConfig(
                (uint)newPaths.Length, newPaths,
                (uint)modes.Length, modes,
                SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_APPLY | SDC_ALLOW_CHANGES);

            if (ret != 0)
            {
                return false;
            }

            _savedPaths = (DISPLAYCONFIG_PATH_INFO[])paths.Clone();
            _savedModes = (DISPLAYCONFIG_MODE_INFO[])modes.Clone();
            return true;
        }

        /// <summary>恢复所有副屏。无已禁用状态时直接返回 false。</summary>
        public static bool RestoreMonitors()
        {
            if (_savedPaths == null || _savedModes == null)
            {
                return false;
            }

            int ret = SetDisplayConfig(
                (uint)_savedPaths.Length, _savedPaths,
                (uint)_savedModes.Length, _savedModes,
                SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_APPLY | SDC_ALLOW_CHANGES);

            if (ret != 0)
            {
                return false;
            }

            _savedPaths = null;
            _savedModes = null;
            return true;
        }

        /// <summary>禁用副屏；若已禁用则恢复。返回操作是否成功。</summary>
        public static bool ToggleDisableSecondary()
        {
            return IsSecondaryDisabled ? RestoreMonitors() : DisableSecondaryMonitors();
        }

        // ---------- 私有辅助 ----------

        private static bool TryGetActiveConfig(out DISPLAYCONFIG_PATH_INFO[] paths, out DISPLAYCONFIG_MODE_INFO[] modes)
        {
            uint pathCount = 0;
            uint modeCount = 0;
            if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, null, ref modeCount, null, out _) != 0)
            {
                paths = null;
                modes = null;
                return false;
            }

            paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            int ret = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, out _);
            if (ret != 0)
            {
                paths = null;
                modes = null;
                return false;
            }
            return true;
        }

        private static DISPLAYCONFIG_MODE_INFO? FindSourceMode(
            DISPLAYCONFIG_MODE_INFO[] modes, DISPLAYCONFIG_PATH_INFO path)
        {
            for (int i = 0; i < modes.Length; i++)
            {
                if (modes[i].infoType == DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE &&
                    modes[i].adapterId.LowPart == path.sourceInfo.adapterId.LowPart &&
                    modes[i].adapterId.HighPart == path.sourceInfo.adapterId.HighPart &&
                    modes[i].id == path.sourceInfo.id)
                {
                    return modes[i];
                }
            }
            return null;
        }

        private static long SourceKey(DISPLAYCONFIG_PATH_SOURCE_INFO info)
        {
            return ((long)(uint)info.adapterId.HighPart << 32) | info.adapterId.LowPart;
        }
    }
}
