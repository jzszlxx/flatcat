#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""FlatCat 发布到 GitHub —— 设备码授权版（不需要手建 token）。

流程：先在终端打印一个 8 位设备码，你在任意设备打开 https://github.com/login/device
输入该码并授权，本脚本拿到一次性 token 后立即建仓 + 推送，token 只存在内存里。

用法：python publish_github_device.py [repo_name]
（脚本会自己起本地代理绕开本机 github.com 阻断）
"""
import json
import os
import socket
import subprocess
import sys
import threading
import time
import urllib.error
import urllib.parse
import urllib.request

CLIENT_ID = "178c6fc778ccc68e1d6a"  # GitHub CLI 的公开 OAuth App，支持设备流
PROXY_PORT = 18080
MAP = {
    "github.com": "140.82.112.3",
    "codeload.github.com": "140.82.112.4",
}
REPO = sys.argv[1].strip() if len(sys.argv) >= 2 and sys.argv[1].strip() else "flatcat"


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
        print("代理端口已占用，复用现有代理", flush=True)
        return
    srv.listen(32)

    def loop():
        while True:
            conn, _ = srv.accept()
            threading.Thread(target=_handle, args=(conn,), daemon=True).start()

    threading.Thread(target=loop, daemon=True).start()
    print("本地代理已启动")


OPENER = urllib.request.build_opener(
    urllib.request.ProxyHandler({"http": "http://127.0.0.1:%d" % PROXY_PORT,
                                 "https": "http://127.0.0.1:%d" % PROXY_PORT}))


def http_json(url, data=None, headers=None):
    body = urllib.parse.urlencode(data).encode() if data else None
    req = urllib.request.Request(url, data=body, method="POST" if data else "GET")
    req.add_header("Accept", "application/json")
    req.add_header("User-Agent", "FlatCat-publisher")
    for k, v in (headers or {}).items():
        req.add_header(k, v)
    try:
        with OPENER.open(req, timeout=30) as r:
            return r.status, json.loads(r.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8", "replace")
        try:
            return e.code, json.loads(raw)
        except Exception:
            return e.code, {"message": raw}


def api(path, token, method="GET", body=None):
    req = urllib.request.Request("https://api.github.com" + path, method=method)
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Authorization", "Bearer " + token)
    req.add_header("X-GitHub-Api-Version", "2022-11-28")
    req.add_header("User-Agent", "FlatCat-publisher")
    data = None
    if body is not None:
        data = json.dumps(body).encode()
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


def main():
    try:
        sys.stdout.reconfigure(line_buffering=True)
        sys.stderr.reconfigure(line_buffering=True)
    except Exception:
        pass
    start_proxy()
    time.sleep(0.3)

    code, dev = http_json("https://github.com/login/device/code",
                          {"client_id": CLIENT_ID, "scope": "repo"})
    if code != 200:
        print("申请设备码失败:", code, dev.get("message"))
        return 2

    print("=" * 56)
    print("请在任意设备打开： %s" % dev["verification_uri"])
    print("输入设备码：       %s" % dev["user_code"])
    print("（%d 分钟内有效，授权后本程序自动建仓并推送）" % (dev["expires_in"] // 60))
    print("=" * 56, flush=True)

    interval = int(dev.get("interval", 5))
    deadline = time.time() + int(dev["expires_in"])
    token = None
    while time.time() < deadline:
        time.sleep(interval)
        code, tok = http_json("https://github.com/login/oauth/access_token",
                              {"client_id": CLIENT_ID,
                               "device_code": dev["device_code"],
                               "grant_type": "urn:ietf:params:oauth:grant-type:device_code"})
        if "access_token" in tok:
            token = tok["access_token"]
            break
        err = tok.get("error", "")
        if err == "authorization_pending":
            continue
        if err == "slow_down":
            interval += 5
            continue
        print("授权失败:", err, tok.get("error_description", ""))
        return 3

    if not token:
        print("设备码已过期，重新运行一次即可")
        return 4

    code, me = api("/user", token)
    if code != 200:
        print("token 校验失败:", code, me.get("message"))
        return 5
    owner = me["login"]
    print("已授权：%s" % owner, flush=True)

    desc = ("会跟着你敲键盘抬爪的 2D 桌面宠物：透明置顶、点击穿透、全局键鼠响应、"
            "番茄钟/喝水/倒计时/多组闹钟、四页设置窗口。C# + WinForms，单文件 exe，MIT。"
            "© 2026 认知觉醒能量站")
    code, repo = api("/repos/%s/%s" % (owner, REPO), token)
    if code == 404:
        code, repo = api("/user/repos", token, "POST", {
            "name": REPO, "description": desc, "private": False,
            "has_issues": True, "has_wiki": False, "license_template": "mit"})
        if code not in (200, 201):
            print("建仓失败:", code, repo.get("message"))
            return 6
        print("已新建仓库：%s" % repo["html_url"], flush=True)
    elif code == 200:
        print("仓库已存在：%s" % repo["html_url"], flush=True)
    else:
        print("查询仓库失败:", code, repo.get("message"))
        return 7

    remote = "https://x-access-token:%s@github.com/%s/%s.git" % (token, owner, REPO)
    env = dict(os.environ)
    env["HTTPS_PROXY"] = "http://127.0.0.1:%d" % PROXY_PORT
    env["HTTP_PROXY"] = env["HTTPS_PROXY"]
    p = subprocess.run(["git", "push", "--set-upstream", remote, "main:main"],
                       cwd=os.path.dirname(os.path.abspath(__file__)),
                       env=env, capture_output=True, text=True)
    print("push:", "成功" if p.returncode == 0 else "失败", flush=True)
    for line in ((p.stderr or p.stdout or "").strip().splitlines())[-6:]:
        print("  " + line.replace(token, "<token>"))

    code, repo = api("/repos/%s/%s" % (owner, REPO), token)
    if code == 200:
        print("仓库地址：%s" % repo["html_url"], flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())