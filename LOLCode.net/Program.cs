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
        v1_1,
        v1_2,
        IRCSPECZ
    }

    internal class LOLProgram
    {
        public LOLCodeVersion version = LOLCodeVersion.v1_2;
        public CompilerParameters compileropts;
        public Scope globals = new Scope();
        public Dictionary<string, LOLMethod> methods = new Dictionary<string, LOLMethod>();
        public List<Assembly> assemblies = new List<Assembly>();

        public LOLProgram(CompilerParameters opts)
        {
            this.compileropts = opts;
            
            UserFunctionRef mainRef = new UserFunctionRef("Main", 0, false);
            mainRef.ReturnType = typeof(void);
            globals.AddSymbol(mainRef);
            methods.Add("Main", new LOLMethod(mainRef, this));

            assemblies.Add(Assembly.GetAssembly(typeof(stdlol.core)));
            ImportLibrary("stdlol.core");
        }

        public MethodInfo Emit(CompilerErrorCollection errors, ModuleBuilder mb)
        { 
            TypeBuilder cls = mb.DefineType(compileropts.MainClass);

            //Define methods
            foreach (LOLMethod method in methods.Values)
                (globals[method.info.Name] as UserFunctionRef).Builder = cls.DefineMethod(method.info.Name, MethodAttributes.Public | MethodAttributes.Static, method.info.ReturnType, method.info.ArgumentTypes);

            //Define globals
            foreach (SymbolRef sr in globals)
                if (sr is GlobalRef)
                    (sr as GlobalRef).Field = cls.DefineField(sr.Name, (sr as GlobalRef).Type, FieldAttributes.Static | FieldAttributes.Public);

            //Emit methods
            foreach (LOLMethod method in methods.Values)
                method.Emit(errors, (globals[method.info.Name] as UserFunctionRef).Builder);

            //Create type
            Type t = cls.CreateType();

            //Return main
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

        public bool ImportLibrary(string name)
        {
            Type t = null;
            foreach (Assembly a in assemblies)
            {
                t = a.GetType(name);
                if (t != null)
                    break;
            }

            if (t == null)
                return false;

            foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                object[] attribs = mi.GetCustomAttributes(typeof(stdlol.LOLCodeFunctionAttribute), true);
                for (int i = 0; i < attribs.Length; i++)
                {
                    stdlol.LOLCodeFunctionAttribute attrib = attribs[i] as stdlol.LOLCodeFunctionAttribute;
                    globals.AddSymbol(new ImportFunctionRef(mi, attrib.Name != null ? attrib.Name : mi.Name));
                }
            }

            return true;
        }
    }

    internal class LOLMethod
    {
        public FunctionRef info;
        public LOLProgram program;
        public Statement statements;
        public List<BreakableStatement> breakables = new List<BreakableStatement>();
        public Scope locals;
        private Dictionary<Type, Stack<LocalBuilder>> tempLocals = new Dictionary<Type, Stack<LocalBuilder>>();

        public LOLMethod(FunctionRef info, LOLProgram prog)
        {
            this.info = info;
            this.program = prog;
            this.locals = new Scope(prog.globals);
        }

        public void DefineLocal(ILGenerator gen, LocalRef l)
        {
            l.Local = gen.DeclareLocal(l.Type);
            if (program.compileropts.IncludeDebugInformation)
                l.Local.SetLocalSymInfo(l.Name);
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

        public void Emit(CompilerErrorCollection errors, MethodBuilder m)
        {
            ILGenerator gen = m.GetILGenerator();
            
            LocalRef it = new LocalRef("IT");
            locals.AddSymbol(it);
            DefineLocal(gen, it);

            statements.Emit(this, gen);
        }
    }
}