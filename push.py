import os
import sys
import subprocess
import datetime

def run_command(command):
    """执行命令并返回输出"""
    print(f"执行命令: {command}")
    try:
        result = subprocess.run(
            command,
            shell=True,
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )
        print(result.stdout)
        return True, result.stdout
    except subprocess.CalledProcessError as e:
        print(f"命令执行失败: {e}")
        print(e.stderr)
        return False, e.stderr

def main():
    # 获取提交信息，如果没有提供则使用默认信息
    commit_message = sys.argv[1] if len(sys.argv) > 1 else f"更新于 {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}"
    
    # 检查git状态
    print("检查git状态...")
    success, output = run_command("git status")
    if not success:
        print("检查git状态失败")
        return
    
    # 如果没有更改，直接退出
    if "nothing to commit, working tree clean" in output:
        print("没有更改需要提交")
        return
    
    # 添加所有更改
    print("添加所有更改...")
    success, _ = run_command("git add .")
    if not success:
        print("添加更改失败")
        return
    
    # 提交更改
    print(f"提交更改: {commit_message}")
    success, _ = run_command(f'git commit -m "{commit_message}"')
    if not success:
        print("提交更改失败")
        return
    
    # 设置远程仓库
    gitee_url = "https://gitee.com/yylmzxc/screen-control.git"
    github_url = "https://github.com/YYLMZXC/screen-control.git"
    
    # 检查并更新远程仓库配置
    print("配置远程仓库...")
    
    # 检查是否已有gitee远程
    success, output = run_command("git remote")
    if not success:
        print("检查远程仓库失败")
        return
    
    # 如果没有gitee远程，添加它
    if "gitee" not in output:
        success, _ = run_command(f"git remote add gitee {gitee_url}")
        if not success:
            print("添加gitee远程失败")
    else:
        # 更新gitee远程URL
        success, _ = run_command(f"git remote set-url gitee {gitee_url}")
        if not success:
            print("更新gitee远程URL失败")
    
    # 如果没有github远程，添加它
    if "github" not in output:
        success, _ = run_command(f"git remote add github {github_url}")
        if not success:
            print("添加github远程失败")
    else:
        # 更新github远程URL
        success, _ = run_command(f"git remote set-url github {github_url}")
        if not success:
            print("更新github远程URL失败")
    
    # 推送到Gitee
    print("推送到Gitee...")
    success, _ = run_command("git push gitee master")
    if success:
        print("成功推送到Gitee")
    else:
        print("推送到Gitee失败")
    
    # 推送到GitHub
    print("推送到GitHub...")
    success, _ = run_command("git push github master")
    if success:
        print("成功推送到GitHub")
    else:
        print("推送到GitHub失败")
    
    print("推送完成！")

if __name__ == "__main__":
    main()