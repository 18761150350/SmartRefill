#!/usr/bin/env python3
"""
智能补货系统 - 生产版本启动脚本

功能：
1. 自动检测端口占用并处理冲突
2. 启动发布版本的静态文件服务器
3. 支持强制终止冲突进程
4. 自动打开浏览器
"""

import os
import sys
import subprocess
import time
import webbrowser
import argparse
from pathlib import Path

def check_port_usage(port):
    """检查端口是否被占用"""
    try:
        result = subprocess.run(['lsof', '-i', f':{port}'], 
                              capture_output=True, text=True, timeout=5)
        if result.returncode == 0 and result.stdout.strip():
            lines = result.stdout.strip().split('\n')[1:]  # 跳过标题行
            processes = []
            for line in lines:
                parts = line.split()
                if len(parts) >= 2:
                    processes.append({'name': parts[0], 'pid': parts[1]})
            return processes
        return []
    except (subprocess.TimeoutExpired, FileNotFoundError):
        return []

def kill_processes_on_port(port, force=False):
    """终止占用端口的进程"""
    processes = check_port_usage(port)
    if not processes:
        return True
    
    if not force:
        print(f"🔍 检测到端口 {port} 被以下进程占用:")
        for proc in processes:
            print(f"   - {proc['name']} (PID: {proc['pid']})")
        
        response = input(f"是否终止这些进程？(y/N): ").lower().strip()
        if response not in ['y', 'yes', '是']:
            return False
    
    print(f"🔥 正在终止端口 {port} 上的进程...")
    for proc in processes:
        try:
            subprocess.run(['kill', '-9', proc['pid']], 
                         capture_output=True, timeout=5)
            print(f"   ✅ 已终止 {proc['name']} (PID: {proc['pid']})")
        except subprocess.TimeoutExpired:
            print(f"   ❌ 无法终止 {proc['name']} (PID: {proc['pid']})")
    
    # 等待进程完全终止
    time.sleep(1)
    
    # 再次检查
    remaining = check_port_usage(port)
    if remaining:
        print(f"⚠️  仍有进程占用端口 {port}")
        return False
    
    print(f"✅ 端口 {port} 已清理完毕")
    return True

def start_server(port=8080, publish_dir="publish/wwwroot", auto_open=True):
    """启动静态文件服务器"""
    
    # 检查发布目录是否存在
    if not os.path.exists(publish_dir):
        print(f"❌ 发布目录不存在: {publish_dir}")
        print("请先运行: dotnet publish -c Release -o ./publish")
        return False
    
    print(f"🚀 启动生产服务器...")
    print(f"📁 静态文件目录: {publish_dir}")
    print(f"🌐 端口: {port}")
    
    try:
        # 使用Python内置的HTTP服务器
        server_cmd = [
            sys.executable, '-m', 'http.server', str(port),
            '--directory', publish_dir
        ]
        
        print(f"💡 执行命令: {' '.join(server_cmd)}")
        print(f"🔗 访问地址: http://localhost:{port}")
        print(f"📱 移动设备访问: http://192.168.0.22:{port} (如果在同一WiFi)")
        print("⏹️  按 Ctrl+C 停止服务器")
        
        if auto_open:
            # 延迟1秒后打开浏览器
            def open_browser():
                time.sleep(1)
                webbrowser.open(f"http://localhost:{port}")
            
            import threading
            threading.Thread(target=open_browser, daemon=True).start()
        
        # 启动服务器
        subprocess.run(server_cmd)
        
    except KeyboardInterrupt:
        print("\n🛑 服务器已停止")
        return True
    except Exception as e:
        print(f"❌ 启动服务器失败: {e}")
        return False

def main():
    parser = argparse.ArgumentParser(description='智能补货系统生产版本启动脚本')
    parser.add_argument('--port', '-p', type=int, default=8080, 
                       help='服务器端口 (默认: 8080)')
    parser.add_argument('--force-kill', '-f', action='store_true',
                       help='强制终止占用端口的进程')
    parser.add_argument('--no-browser', action='store_true',
                       help='不自动打开浏览器')
    parser.add_argument('--publish-dir', default='publish/wwwroot',
                       help='发布目录路径 (默认: publish/wwwroot)')
    
    args = parser.parse_args()
    
    print("🎯 智能补货系统 - 生产版本启动器")
    print("=" * 50)
    
    # 检查端口占用
    if check_port_usage(args.port):
        print(f"⚠️  端口 {args.port} 被占用")
        if not kill_processes_on_port(args.port, args.force_kill):
            print(f"❌ 无法清理端口 {args.port}，请尝试其他端口或使用 --force-kill")
            return 1
    
    # 启动服务器
    success = start_server(
        port=args.port, 
        publish_dir=args.publish_dir,
        auto_open=not args.no_browser
    )
    
    return 0 if success else 1

if __name__ == '__main__':
    sys.exit(main()) 