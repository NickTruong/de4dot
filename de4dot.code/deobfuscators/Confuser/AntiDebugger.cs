﻿/*
    Copyright (C) 2011-2012 de4dot@gmail.com

    This file is part of de4dot.

    de4dot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    de4dot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with de4dot.  If not, see <http://www.gnu.org/licenses/>.
*/

using Mono.Cecil;
using Mono.Cecil.Cil;
using de4dot.blocks;

namespace de4dot.code.deobfuscators.Confuser {
	class AntiDebugger {
		ModuleDefinition module;
		MethodDefinition initMethod;

		public MethodDefinition InitMethod {
			get { return initMethod; }
		}

		public TypeDefinition Type {
			get { return initMethod != null ? initMethod.DeclaringType : null; }
		}

		public bool Detected {
			get { return initMethod != null; }
		}

		public AntiDebugger(ModuleDefinition module) {
			this.module = module;
		}

		public void find() {
			if (checkMethod(DotNetUtils.getModuleTypeCctor(module)))
				return;
		}

		bool checkMethod(MethodDefinition method) {
			if (method == null || method.Body == null)
				return false;

			foreach (var instr in method.Body.Instructions) {
				if (instr.OpCode.Code != Code.Call)
					continue;
				var calledMethod = instr.Operand as MethodDefinition;
				if (calledMethod == null)
					continue;
				if (!DotNetUtils.isMethod(calledMethod, "System.Void", "()"))
					continue;

				if (checkInitMethod(calledMethod)) {
					initMethod = calledMethod;
					return true;
				}
			}
			return false;
		}

		static bool checkInitMethod(MethodDefinition method) {
			if (method == null || method.Body == null || !method.IsStatic)
				return false;
			if (!DotNetUtils.isMethod(method, "System.Void", "()"))
				return false;
			if (!DotNetUtils.hasString(method, "COR_ENABLE_PROFILING"))
				return false;
			if (!DotNetUtils.hasString(method, "COR_PROFILER"))
				return false;
			if (!DotNetUtils.hasString(method, "Profiler detected"))
				return false;
			if (DotNetUtils.getPInvokeMethod(method.DeclaringType, "ntdll", "NtQueryInformationProcess") == null)
				return false;
			if (DotNetUtils.getPInvokeMethod(method.DeclaringType, "ntdll", "NtSetInformationProcess") == null)
				return false;
			if (DotNetUtils.getPInvokeMethod(method.DeclaringType, "kernel32", "CloseHandle") == null)
				return false;
			if (DotNetUtils.getPInvokeMethod(method.DeclaringType, "kernel32", "IsDebuggerPresent") == null)
				return false;
			if (DotNetUtils.getPInvokeMethod(method.DeclaringType, "kernel32", "OutputDebugString") == null)
				return false;

			return true;
		}
	}
}
