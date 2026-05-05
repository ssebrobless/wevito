"""Visible OS-level driver for the user's already-logged-in Gemini Chrome window.

This is intentionally separate from the CDP helper. It does not require Chrome to
be launched with remote debugging, so it can operate on an already-authenticated
browser session when Google blocks a fresh automation-profile login.
"""

from __future__ import annotations

import argparse
import ctypes
import json
import time
from dataclasses import asdict, dataclass
from io import BytesIO
from pathlib import Path
from typing import Iterable

import pyautogui
import pyperclip
import win32clipboard
import win32con
from PIL import Image
from ctypes import wintypes


GEMINI_TITLE_MARKER = "Google Gemini"
DEFAULT_GEMINI_URL = "https://gemini.google.com/u/1/app"


class RECT(ctypes.Structure):
    _fields_ = [
        ("left", ctypes.c_long),
        ("top", ctypes.c_long),
        ("right", ctypes.c_long),
        ("bottom", ctypes.c_long),
    ]


EnumWindowsProc = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
user32 = ctypes.windll.user32
user32.EnumWindows.argtypes = [EnumWindowsProc, wintypes.LPARAM]
user32.EnumWindows.restype = wintypes.BOOL
user32.IsWindowVisible.argtypes = [wintypes.HWND]
user32.IsWindowVisible.restype = wintypes.BOOL
user32.GetWindowTextLengthW.argtypes = [wintypes.HWND]
user32.GetWindowTextLengthW.restype = ctypes.c_int
user32.GetWindowTextW.argtypes = [wintypes.HWND, wintypes.LPWSTR, ctypes.c_int]
user32.GetWindowTextW.restype = ctypes.c_int
user32.GetWindowRect.argtypes = [wintypes.HWND, ctypes.POINTER(RECT)]
user32.GetWindowRect.restype = wintypes.BOOL
user32.ShowWindow.argtypes = [wintypes.HWND, ctypes.c_int]
user32.BringWindowToTop.argtypes = [wintypes.HWND]
user32.SetForegroundWindow.argtypes = [wintypes.HWND]
user32.MoveWindow.argtypes = [wintypes.HWND, ctypes.c_int, ctypes.c_int, ctypes.c_int, ctypes.c_int, wintypes.BOOL]


@dataclass
class WindowInfo:
    handle: int
    title: str
    left: int
    top: int
    right: int
    bottom: int

    @property
    def width(self) -> int:
        return self.right - self.left

    @property
    def height(self) -> int:
        return self.bottom - self.top


def iter_visible_windows() -> Iterable[WindowInfo]:
    windows: list[WindowInfo] = []

    def callback(hwnd: wintypes.HWND, _lparam: wintypes.LPARAM) -> bool:
        if not user32.IsWindowVisible(hwnd):
            return True

        length = user32.GetWindowTextLengthW(hwnd)
        if length <= 0:
            return True

        buffer = ctypes.create_unicode_buffer(length + 1)
        user32.GetWindowTextW(hwnd, buffer, length + 1)
        title = buffer.value
        if not title:
            return True

        rect = RECT()
        if not user32.GetWindowRect(hwnd, ctypes.byref(rect)):
            return True

        windows.append(
            WindowInfo(
                handle=int(hwnd),
                title=title,
                left=rect.left,
                top=rect.top,
                right=rect.right,
                bottom=rect.bottom,
            )
        )
        return True

    user32.EnumWindows(EnumWindowsProc(callback), 0)
    return windows


def find_gemini_window() -> WindowInfo:
    candidates = [window for window in iter_visible_windows() if GEMINI_TITLE_MARKER in window.title]
    if not candidates:
        raise RuntimeError("No visible Google Gemini window found.")
    return max(candidates, key=lambda window: window.width * window.height)


def focus_window(window: WindowInfo) -> WindowInfo:
    hwnd = wintypes.HWND(window.handle)
    user32.ShowWindow(hwnd, 9)  # SW_RESTORE
    user32.BringWindowToTop(hwnd)
    user32.SetForegroundWindow(hwnd)
    time.sleep(0.4)
    return find_gemini_window()


def move_window(window: WindowInfo, left: int, top: int, width: int, height: int) -> WindowInfo:
    hwnd = wintypes.HWND(window.handle)
    user32.ShowWindow(hwnd, 9)
    user32.MoveWindow(hwnd, left, top, width, height, True)
    time.sleep(0.4)
    return find_gemini_window()


def click_relative(window: WindowInfo, x_fraction: float, y_fraction: float) -> None:
    pyautogui.click(window.left + window.width * x_fraction, window.top + window.height * y_fraction)
    time.sleep(0.25)


def paste_text(text: str) -> None:
    pyperclip.copy(text)
    pyautogui.hotkey("ctrl", "v")
    time.sleep(0.25)


def copy_image_to_clipboard(image_path: Path) -> None:
    image = Image.open(image_path).convert("RGB")
    output = BytesIO()
    image.save(output, "BMP")
    dib = output.getvalue()[14:]
    output.close()

    win32clipboard.OpenClipboard()
    try:
        win32clipboard.EmptyClipboard()
        win32clipboard.SetClipboardData(win32con.CF_DIB, dib)
    finally:
        win32clipboard.CloseClipboard()


def paste_image(image_path: Path) -> None:
    copy_image_to_clipboard(image_path)
    pyautogui.hotkey("ctrl", "v")
    time.sleep(1.0)


def save_screenshot(window: WindowInfo, output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    image = pyautogui.screenshot(region=(window.left, window.top, window.width, window.height))
    image.save(output)


def command_status(args: argparse.Namespace) -> None:
    windows = [asdict(window) for window in iter_visible_windows()]
    print(json.dumps(windows, indent=2))


def command_focus(args: argparse.Namespace) -> None:
    window = focus_window(find_gemini_window())
    print(json.dumps(asdict(window), indent=2))


def command_layout(args: argparse.Namespace) -> None:
    window = find_gemini_window()
    window = move_window(window, args.left, args.top, args.width, args.height)
    window = focus_window(window)
    print(json.dumps(asdict(window), indent=2))


def command_screenshot(args: argparse.Namespace) -> None:
    window = focus_window(find_gemini_window())
    save_screenshot(window, Path(args.output))
    print(json.dumps({"screenshot": args.output, "window": asdict(window)}, indent=2))


def command_navigate(args: argparse.Namespace) -> None:
    window = focus_window(find_gemini_window())
    pyautogui.hotkey("ctrl", "l")
    time.sleep(0.15)
    paste_text(args.url)
    pyautogui.press("enter")
    time.sleep(args.wait_seconds)
    window = focus_window(find_gemini_window())
    print(json.dumps({"navigated": args.url, "window": asdict(window)}, indent=2))


def command_paste_prompt(args: argparse.Namespace) -> None:
    prompt = Path(args.prompt).read_text(encoding="utf-8")
    window = focus_window(find_gemini_window())
    click_relative(window, args.x_fraction, args.y_fraction)
    paste_text(prompt)
    print(json.dumps({"pasted_prompt": args.prompt, "window": asdict(window)}, indent=2))


def command_paste_image(args: argparse.Namespace) -> None:
    window = focus_window(find_gemini_window())
    click_relative(window, args.x_fraction, args.y_fraction)
    paste_image(Path(args.image))
    print(json.dumps({"pasted_image": args.image, "window": asdict(window)}, indent=2))


def command_paste_job(args: argparse.Namespace) -> None:
    prompt = Path(args.prompt).read_text(encoding="utf-8")
    window = focus_window(find_gemini_window())
    click_relative(window, args.x_fraction, args.y_fraction)
    paste_image(Path(args.image))
    paste_text("\n\n" + prompt)
    print(
        json.dumps(
            {
                "pasted_image": args.image,
                "pasted_prompt": args.prompt,
                "window": asdict(window),
                "submitted": args.submit,
            },
            indent=2,
        )
    )
    if args.submit:
        pyautogui.press("enter")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    status = subparsers.add_parser("status")
    status.set_defaults(func=command_status)

    focus = subparsers.add_parser("focus")
    focus.set_defaults(func=command_focus)

    layout = subparsers.add_parser("layout")
    layout.add_argument("--left", type=int, default=960)
    layout.add_argument("--top", type=int, default=40)
    layout.add_argument("--width", type=int, default=960)
    layout.add_argument("--height", type=int, default=1000)
    layout.set_defaults(func=command_layout)

    screenshot = subparsers.add_parser("screenshot")
    screenshot.add_argument("--output", required=True)
    screenshot.set_defaults(func=command_screenshot)

    navigate = subparsers.add_parser("navigate")
    navigate.add_argument("--url", default=DEFAULT_GEMINI_URL)
    navigate.add_argument("--wait-seconds", type=float, default=3.0)
    navigate.set_defaults(func=command_navigate)

    paste_prompt = subparsers.add_parser("paste-prompt")
    paste_prompt.add_argument("--prompt", required=True)
    paste_prompt.add_argument("--x-fraction", type=float, default=0.50)
    paste_prompt.add_argument("--y-fraction", type=float, default=0.56)
    paste_prompt.set_defaults(func=command_paste_prompt)

    paste_image_parser = subparsers.add_parser("paste-image")
    paste_image_parser.add_argument("--image", required=True)
    paste_image_parser.add_argument("--x-fraction", type=float, default=0.52)
    paste_image_parser.add_argument("--y-fraction", type=float, default=0.56)
    paste_image_parser.set_defaults(func=command_paste_image)

    paste_job = subparsers.add_parser("paste-job")
    paste_job.add_argument("--image", required=True)
    paste_job.add_argument("--prompt", required=True)
    paste_job.add_argument("--x-fraction", type=float, default=0.52)
    paste_job.add_argument("--y-fraction", type=float, default=0.56)
    paste_job.add_argument("--submit", action="store_true")
    paste_job.set_defaults(func=command_paste_job)

    return parser


def main() -> None:
    args = build_parser().parse_args()
    pyautogui.PAUSE = 0.08
    args.func(args)


if __name__ == "__main__":
    main()
