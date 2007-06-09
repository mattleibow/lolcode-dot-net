using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Reflection.Emit;
using System.Reflection;
using System;
namespace notdot.LOLCode
{
    internal enum LOLCodeVersion
    {
        v1_0,
        IRCSPECZ
    }

    internal class LOLProgram
    {
        public LOLCodeVersion version = LOLCodeVersion.IRCSPECZ;
        public CompilerParameters compileropts;
        public Dictionary<string, LOLMethod> methods = new Dictionary<string, LOLMethod>();

        public LOLProgram(CompilerParameters opts)
        {
            this.compileropts = opts;
            methods.Add("Main", new LOLMethod("Main", this));
        }

        public MethodInfo Emit(CompilerErrorCollection errors, ModuleBuilder mb)
        { 
            TypeBuilder cls = mb.DefineType(compileropts.MainClass);

            foreach(LOLMethod method in methods.Values)
                method.Emit(errors, cls);

            Type t = cls.CreateType();
            return t.GetMethod("Main");
        }

        public static void WrapObject(Type t, ILGenerator gen)
        {
            if (t == typeof(int))
            {
                //Box the int
                gen.Emit(OpCodes.Box, typeof(int));
            }
            else if (t == typeof(Dictionary<object, object>))
            {
                //Clone the array
                gen.Emit(OpCodes.Newobj, typeof(Dictionary<object, object>).GetConstructor(new Type[] { typeof(Dictionary<object, object>) }));
            }
        }
    }

    internal class LOLMethod
    {
        public string name;
        public LOLProgram program;
        public Statement statements;
        public List<BreakableStatement> breakables = new List<BreakableStatement>();
        private List<Dictionary<string, LocalBuilder>> locals = new List<Dictionary<string, LocalBuilder>>();
        private Dictionary<Type, Stack<LocalBuilder>> tempLocals = new Dictionary<Type, Stack<LocalBuilder>>();

        public LOLMethod(string name, LOLProgram prog)
        {
            this.name = name;
            this.program = prog;
        }

        public void BeginScope(ILGenerator gen)
        {
            gen.BeginScope();
            locals.Add(new Dictionary<string, LocalBuilder>());
        }

        public void EndScope(ILGenerator gen)
        {
            locals.RemoveAt(locals.Count - 1);
            gen.EndScope();
        }

        public LocalBuilder CreateLocal(string name, ILGenerator gen)
        {
            LocalBuilder ret = gen.DeclareLocal(typeof(object));
            if (program.compileropts.IncludeDebugInformation)
                ret.SetLocalSymInfo(name);
            locals[locals.Count - 1].Add(name, ret);

            return ret;
        }

        public LocalBuilder GetLocal(string name)
        {
            LocalBuilder ret;

            for (int i = locals.Count - 1; i >= 0; i--)
                if (locals[i].TryGetValue(name, out ret))
                    return ret;

            throw new KeyNotFoundException();
        }

        public LocalBuilder GetTempLocal(ILGenerator gen, Type t)
        {
            Stack<LocalBuilder> temps;
            if (tempLocals.TryGetValue(t, out temps) && temps.Count > 0)
            {
                return temps.Pop();
            }
            else
            {
                return gen.DeclareLocal(t);
            }
        }

        public void ReleaseTempLocal(LocalBuilder lb)
        {
            Stack<LocalBuilder> temps;
            if (!tempLocals.TryGetValue(lb.LocalType, out temps))
            {
                temps = new Stack<LocalBuilder>();
                tempLocals.Add(lb.LocalType, temps);
            }

            temps.Push(lb);
        }

        public void Emit(CompilerErrorCollection errors, TypeBuilder cls)
        {
            MethodBuilder m = cls.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { });
            ILGenerator gen = m.GetILGenerator();

            statements.Process(this, errors, gen);

            BeginScope(gen);

            statements.Emit(this, gen);

            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);

            EndScope(gen);
        }
    }
}