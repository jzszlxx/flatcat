#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""FlatCat 一键发布到 GitHub。

用法（token 不会写入任何文件）：
    GITHUB_TOKEN=ghp_xxx python publish_github.py
    python publish_github.py ghp_xxx [repo_name]

做的事：
1. 起一个本地 CONNECT 代理（github.com 在本机被阻断，只有部分 IP 可连）
2. 用 api.github.com 校验 token、查当前用户
3. 仓库不存在就新建（public，MIT）
4. 用 token 临时 URL 推送 main（不写进 .git/config）
5. 回读仓库信息做校验

前置：本目录已 git init 并至少有一次提交（FlatCat 已如此）。
安全：token 只在本进程内存与 git 子进程命令行里，不落盘；用完请在 GitHub 设置里撤销。
"""
import json
import os
import re
import socket
import subprocess
import sys
import threading
import time
import urllib.error
import urllib.request

API = "https://api.github.com"
PROXY_PORT = 18080
MAP = {
    "github.com": "140.82.112.3",
    "codeload.github.com": "140.82.112.4",
}


# ---------------------------------------------------------------- 本地代理
def _pump(a, b):
    def fwd(src, dst):
        try:
            while True:
                data = src.recv(65536)
                if not data:
                    break
                dst.sendall(data)
        except Exception:
            pass
        finally:
            try:
                dst.shutdown(socket.SHUT_WR)
            except Exception:
                pass

    t = threading.Thread(target=fwd, args=(b, a), daemon=True)
    t.start()
    fwd(a, b)
    t.join(timeout=1)


def _handle(conn):
    head = b""
    try:
        while b"\r\n\r\n" not in head:
            chunk = conn.recv(4096)
            if not chunk:
                return
            head += chunk
        first = head.split(b"\r\n\r\n", 1)[0].decode("latin1").split("\r\n")[0]
        parts = first.split(" ")
        if len(parts) < 2 or parts[0].upper() != "CONNECT":
            conn.sendall(b"HTTP/1.1 405 Only CONNECT\r\n\r\n")
            return
        host, port = parts[1].rsplit(":", 1)
        up = socket.create_connection((MAP.get(host, host), int(port)), timeout=25)
        conn.sendall(b"HTTP/1.1 200 Connection Established\r\n\r\n")
        rest = head.split(b"\r\n\r\n", 1)[1]
        if rest:
            up.sendall(rest)
        _pump(conn, up)
        up.close()
    except Exception:
        pass
    finally:
        try:
            conn.close()
        except Exception:
            pass


def start_proxy():
    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    try:
        srv.bind(("127.0.0.1", PROXY_PORT))
    except OSError:
        print("代理端口已被占用，直接复用现有代理")
        return
    srv.listen(32)

    def loop():
        while True:
            conn, _ = srv.accept()
            threading.Thread(target=_handle, args=(conn,), daemon=True).start()

    threading.Thread(target=loop, daemon=True).start()
    print("本地代理已启动 127.0.0.1:%d" % PROXY_PORT)


# ---------------------------------------------------------------- API
def api(path, token, method="GET", body=None):
    req = urllib.request.Request(API + path, method=method)
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Authorization", "Bearer " + token)
    req.add_header("X-GitHub-Api-Version", "2022-11-28")
    req.add_header("User-Agent", "FlatCat-publisher")
    data = None
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, data=data, timeout=30) as r:
            raw = r.read().decode("utf-8")
            return r.status, (json.loads(raw) if raw.strip() else {})
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8", "replace")
        try:
            return e.code, json.loads(raw)
        except Exception:
            return e.code, {"message": raw}


def git(args, cwd="."):
    p = subprocess.run(["git"] + args, cwd=cwd, capture_output=True, text=True)
    out = (p.stdout or "").strip()
    err = (p.stderr or "").strip()
    if p.returncode != 0:
        raise RuntimeError("git %s 失败: %s" % (" ".join(args), err or out))
    return out


def main():
    token = None
    repo = "flatcat"
    if len(sys.argv) >= 2 and sys.argv[1].strip():
        token = sys.argv[1].strip()
    if not token:
        token = os.environ.get("GITHUB_TOKEN", "").strip()
    if len(sys.argv) >= 3 and sys.argv[2].strip():
        repo = re.sub(r"[^A-Za-z0-9._-]", "-", sys.argv[2].strip())
    if not token:
        print("缺少 token：GITHUB_TOKEN=xxx python publish_github.py")
        return 2

    start_proxy()
    time.sleep(0.3)

    code, me = api("/user", token)
    if code != 200:
        print("token 无效或权限不足：%s %s" % (code, me.get("message")))
        return 3
    owner = me["login"]
    print("已认证：%s（%s）" % (owner, me.get("name") or "无昵称"))

    desc = ("会跟着你敲键盘抬爪的 2D 桌面宠物：透明置顶、点击穿透、全局键鼠响应、"
            "番茄钟/喝水/倒计时/多组闹钟、四页设置窗口。C# + WinForms，单文件 exe，MIT。"
            "© 2026 认知觉醒能量站")
    code, payload = api("/repos/%s/%s" % (owner, repo), token)
    if code == 404:
        code, payload = api("/user/repos", token, "POST", {
            "name": repo,
            "description": desc,
            "private": False,
            "has_issues": True,
            "has_wiki": False,
            "license_template": "mit",
        })
        if code not in (200, 201):
            print("建仓失败：%s %s" % (code, payload.get("message")))
            return 4
        print("已新建仓库：%s" % payload["html_url"])
    elif code == 200:
        print("仓库已存在，直接推送：%s" % payload["html_url"])
    else:
        print("查询仓库失败：%s %s" % (code, payload.get("message")))
        return 5

    remote = "https://x-access-token:%s@github.com/%s/%s.git" % (token, owner, repo)
    env = dict(os.environ)
    env["HTTPS_PROXY"] = "http://127.0.0.1:%d" % PROXY_PORT
    env["HTTP_PROXY"] = env["HTTPS_PROXY"]
    p = subprocess.run(["git", "push", "--set-upstream", remote, "main:main"],
                       cwd=".", env=env, capture_output=True, text=True)
    ok = p.returncode == 0
    tail = (p.stderr or p.stdout or "").strip().splitlines()
    print("push:", "成功" if ok else "失败")
    for line in tail[-6:]:
        line = line.replace(token, "<token>")
        print("  " + line)
    if not ok:
        return 6

    code, payload = api("/repos/%s/%s", token)
    if code == 200:
        print("仓库地址：%s" % payload["html_url"])
        print("默认分支：%s  stars=%s" % (payload.get("default_branch"), payload.get("stargazers_count", 0)))
    return 0


if __name__ == "__main__":
    sys.exit(main())