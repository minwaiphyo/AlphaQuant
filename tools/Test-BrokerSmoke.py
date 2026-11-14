#!/usr/bin/env python3
"""Loopback-only, synthetic broker process checks. Never contacts Alpaca."""
import base64
import hashlib
import json
import os
from pathlib import Path
import socket
import subprocess
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
DLL = ROOT / 'src/AlphaQuant.OAuthBroker/bin/Release/net10.0/AlphaQuant.OAuthBroker.dll'

def main():
    with socket.socket() as listener:
        listener.bind(('127.0.0.1', 0))
        port = listener.getsockname()[1]
    origin = f'http://127.0.0.1:{port}'
    environment = dict(os.environ, ASPNETCORE_URLS=origin,
        AQ_ALPACA_CLIENT_ID='synthetic-client', AQ_ALPACA_CLIENT_SECRET='synthetic-not-a-credential',
        AQ_OAUTH_REDIRECT_URI='https://broker.example/oauth/callback')
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    def request(path, payload=None):
        data = None if payload is None else json.dumps(payload).encode()
        req = urllib.request.Request(origin + path, data=data,
            headers={'Content-Type': 'application/json'} if data else {})
        try:
            response = opener.open(req, timeout=3)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            return response.status, response.headers, response.read()
    def start():
        process = subprocess.Popen(['dotnet', str(DLL)], env=environment,
            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        try:
            for _ in range(100):
                if process.poll() is not None:
                    raise RuntimeError('Broker exited before readiness')
                try:
                    if request('/health')[0] == 200:
                        return process
                except (OSError, urllib.error.URLError):
                    pass
                time.sleep(.05)
            raise RuntimeError('Broker readiness deadline exceeded')
        except BaseException:
            stop(process)
            raise
    def stop(process):
        if process.poll() is None:
            process.terminate()
            try: process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                process.kill(); process.wait()
    verifier = 'a' * 43
    challenge = base64.urlsafe_b64encode(hashlib.sha256(verifier.encode()).digest()).decode().rstrip('=')
    process = start()
    try:
        code, headers, body = request('/health')
        assert code == 200 and json.loads(body)['status'] == 'ready'
        assert headers['Cache-Control'] == 'no-store' and headers['Referrer-Policy'] == 'no-referrer'
        assert request('/sessions', {'challenge': 'invalid'})[0] == 400
        code, _, body = request('/sessions', {'challenge': challenge})
        assert code == 200
        session = json.loads(body)
        assert 'scope=data' in session['authorizationUrl'] and 'trading' not in session['authorizationUrl']
        path = '/sessions/' + session['sessionId']
        assert request(path + '/result', {'verifier': 'b' * 43})[0] == 404
        assert request(path + '/result', {'verifier': verifier})[0] == 202
        assert request('/oauth/callback?state=' + session['sessionId'] + '&error=access_denied')[0] == 200
        assert request(path + '/result', {'verifier': verifier})[0] == 400
        assert request(path + '/result', {'verifier': verifier})[0] == 404
        code, _, body = request('/sessions', {'challenge': challenge})
        session = json.loads(body)
        pending = '/sessions/' + session['sessionId'] + '/result'
        limited = False
        for _ in range(95):
            if request('/health')[0] == 429:
                limited = True; break
        assert limited, 'Expected rate limit without a queue'
        stop(process); process = start()
        assert request(pending, {'verifier': verifier})[0] == 404, 'Restart must lose pending sessions'
        print('Broker smoke passed: health/headers, invalid challenge, ownership, denial, one-time result, rate limit and restart loss; no provider calls.')
    finally:
        stop(process)

if __name__ == '__main__':
    main()
