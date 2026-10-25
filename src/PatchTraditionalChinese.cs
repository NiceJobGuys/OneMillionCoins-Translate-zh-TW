using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public static class PatchTraditionalChinese
{
    public static void Build(string source, string translation, string additions, string output)
    {
        using (var resolver = new DefaultAssemblyResolver())
        {
            resolver.AddSearchDirectory(Path.GetDirectoryName(source));
            using (var game = AssemblyDefinition.ReadAssembly(source, new ReaderParameters { AssemblyResolver = resolver }))
            {
            var localization = game.MainModule.Types.Single(t => t.FullName == "GoldDigger.GameLocalization");
            var option = localization.NestedTypes.Single(t => t.Name == "LanguageOption");
            var initializer = localization.Methods.Single(m => m.Name == ".cctor");
            if (initializer.Body.Instructions[0].OpCode != OpCodes.Ldc_I4_4 ||
                initializer.Body.Instructions.Count(i => i.OpCode == OpCodes.Newobj) != 4)
                throw new InvalidOperationException("Unexpected language list; refusing to patch.");
            initializer.Body.Instructions[0].OpCode = OpCodes.Ldc_I4_5;
            var constructor = option.Methods.Single(m => m.Name == ".ctor");
            var insert = initializer.Body.Instructions.Single(i => i.Operand is MethodReference && ((MethodReference)i.Operand).Name == "AsReadOnly");
            var il = initializer.Body.GetILProcessor();
            foreach (var instruction in new[] {
                il.Create(OpCodes.Dup), il.Create(OpCodes.Ldc_I4_4),
                il.Create(OpCodes.Ldstr, "zh-Hant"), il.Create(OpCodes.Ldstr, "繁體中文"),
                il.Create(OpCodes.Ldstr, "Localization/zh-Hant"), il.Create(OpCodes.Ldstr, "Localization/zh-Hant-additions"),
                il.Create(OpCodes.Newobj, constructor), il.Create(OpCodes.Stelem_Ref)
            }) il.InsertBefore(insert, instruction);

            var getter = option.Methods.Single(m => m.Name == "get_Catalog");
            var catalog = option.Fields.Single(f => f.Name == "_catalog");
            var id = option.Methods.Single(m => m.Name == "get_Id");
            var originalLoad = getter.Body.Instructions.First(i => i.Operand is MethodReference && ((MethodReference)i.Operand).Name == "Load").Previous.Previous;
            var cacheMiss = getter.Body.Instructions.Single(i => i.OpCode == OpCodes.Brfalse_S && i.Operand == originalLoad);
            var fromJson = (MethodReference)getter.Body.Instructions.Single(i => i.Operand is MethodReference && ((MethodReference)i.Operand).Name == "FromJson").Operand;
            var isChinese = localization.Methods.Single(m => m.Name == "get_IsSimplifiedChinese");
            var equality = (MethodReference)isChinese.Body.Instructions.Single(i => i.Operand is MethodReference && ((MethodReference)i.Operand).Name == "op_Equality").Operand;
            il = getter.Body.GetILProcessor();
            var first = il.Create(OpCodes.Ldarg_0);
            foreach (var instruction in new[] {
                first, il.Create(OpCodes.Call, id), il.Create(OpCodes.Ldstr, "zh-Hant"),
                il.Create(OpCodes.Call, equality), il.Create(OpCodes.Brfalse, originalLoad),
                il.Create(OpCodes.Ldarg_0), il.Create(OpCodes.Ldstr, File.ReadAllText(translation)),
                il.Create(OpCodes.Ldstr, "zh-Hant"), il.Create(OpCodes.Ldstr, File.ReadAllText(additions)),
                il.Create(OpCodes.Call, fromJson), il.Create(OpCodes.Stfld, catalog),
                il.Create(OpCodes.Ldarg_0), il.Create(OpCodes.Ldfld, catalog), il.Create(OpCodes.Ret)
            }) il.InsertBefore(originalLoad, instruction);
            cacheMiss.Operand = first;

            // Both existing callers use this predicate for Chinese fonts and input hints.
            var getLanguage = localization.Methods.Single(m => m.Name == "get_Language");
            il = isChinese.Body.GetILProcessor();
            var oldFirst = isChinese.Body.Instructions[0];
            foreach (var instruction in new[] {
                il.Create(OpCodes.Call, getLanguage), il.Create(OpCodes.Ldstr, "zh-Hant"),
                il.Create(OpCodes.Call, equality), il.Create(OpCodes.Brfalse, oldFirst),
                il.Create(OpCodes.Ldc_I4_1), il.Create(OpCodes.Ret)
            }) il.InsertBefore(oldFirst, instruction);

            var prompts = game.MainModule.Types.Single(t => t.Name == "InputPrompts").Methods.Single(m => m.Name == "FormatLocalizedBindings");
            var lastReplace = prompts.Body.Instructions.First(i => (i.Operand as string) == "{Secondary}").Next;
            if (!(lastReplace.Operand is MethodReference) || ((MethodReference)lastReplace.Operand).Name != "Replace" || lastReplace.Next.OpCode != OpCodes.Starg_S)
                throw new InvalidOperationException("Unexpected Chinese input hints.");
            il = prompts.Body.GetILProcessor();
            var store = lastReplace.Next;
            foreach (var instruction in new[] {
                il.Create(OpCodes.Ldstr, "滑鼠左鍵"), il.Create(OpCodes.Ldstr, "{Primary}"), il.Create(OpCodes.Callvirt, (MethodReference)lastReplace.Operand),
                il.Create(OpCodes.Ldstr, "滑鼠右鍵"), il.Create(OpCodes.Ldstr, "{Secondary}"), il.Create(OpCodes.Callvirt, (MethodReference)lastReplace.Operand)
            }) il.InsertBefore(store, instruction);

            foreach (var method in new[] { initializer, getter, isChinese, prompts })
                foreach (var instruction in method.Body.Instructions)
                    if (instruction.OpCode.OperandType == OperandType.ShortInlineBrTarget)
                    {
                        var name = instruction.OpCode.Code.ToString();
                        instruction.OpCode = (OpCode)typeof(OpCodes).GetField(name.Substring(0, name.Length - 2)).GetValue(null);
                    }
            game.Write(output);
            }
        }
        using (var check = AssemblyDefinition.ReadAssembly(output))
        {
            var localization = check.MainModule.Types.Single(t => t.Name == "GameLocalization");
            var initializer = localization.Methods.Single(m => m.Name == ".cctor");
            if (initializer.Body.Instructions[0].OpCode != OpCodes.Ldc_I4_5 ||
                initializer.Body.Instructions.Count(i => i.OpCode == OpCodes.Newobj) != 5 ||
                !initializer.Body.Instructions.Any(i => (i.Operand as string) == "简体中文") ||
                !initializer.Body.Instructions.Any(i => (i.Operand as string) == "繁體中文"))
                throw new InvalidOperationException("Language registration verification failed.");
        }
        Console.WriteLine("PASS: fifth language, cached catalog, Chinese fonts and rebinding support written and read back.");
    }
}
