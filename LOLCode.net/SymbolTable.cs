using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace notdot.LOLCode
{
    internal abstract class SymbolRef
    {
        public string Name;
    }

    internal abstract class FunctionRef : SymbolRef
    {
        public int Arity;
        public bool IsVariadic;
        public Type ReturnType;
        public Type[] ArgumentTypes;
        public abstract MethodInfo Method { get; }
    }

    internal class UserFunctionRef : FunctionRef
    {
        public MethodBuilder Builder;

        public override MethodInfo Method
        {
            get { return Builder; }
        }

        public UserFunctionRef(string name, int arity, bool variadic)
        {
            this.Name = name;
            this.Arity = arity;
            this.IsVariadic = variadic;
            this.Builder = null;
            this.ReturnType = typeof(object);

            this.ArgumentTypes = new Type[arity];
            for (int i = 0; i < arity; i++)
                this.ArgumentTypes[i] = typeof(object);
        }
    }

    internal class ImportFunctionRef : FunctionRef
    {
        private MethodInfo m_Method;

        public override MethodInfo Method
        {
            get { return m_Method; }
        }

        public ImportFunctionRef(MethodInfo m, string name)
        {
            this.Name = name;

            ParameterInfo[] pi = m.GetParameters();
            if (pi[pi.Length - 1].GetCustomAttributes(typeof(ParamArrayAttribute), true).Length > 0)
            {
                this.Arity = pi.Length - 1;
                this.IsVariadic = true;
            }
            else
            {
                this.Arity = pi.Length;
                this.IsVariadic = false;
            }

            this.m_Method = m;
            this.ReturnType = m.ReturnType;

            this.ArgumentTypes = new Type[pi.Length];
            for (int i = 0; i < pi.Length; i++)
                this.ArgumentTypes[i] = pi[i].ParameterType;
        }
    }

    internal abstract class VariableRef : SymbolRef
    {
        public Type Type;
    }

    internal class GlobalRef : VariableRef
    {
        public FieldBuilder Field;

        public GlobalRef(string name)
        {
            this.Name = name;
            this.Field = null;
            this.Type = typeof(object);
        }
    }

    internal class LocalRef : VariableRef
    {
        public LocalBuilder Local;

        public LocalRef(string name)
        {
            this.Name = name;
            this.Local = null;
            this.Type = typeof(object);
        }
    }

    internal class ArgumentRef : VariableRef
    {
        public short Number;

        public ArgumentRef(string name, short num)
        {
            this.Name = name;
            this.Number = num;
            this.Type = typeof(object);
        }
    }

    internal class Scope : IEnumerable<SymbolRef>
    {
        private Scope parentScope = null;
        private Dictionary<string, SymbolRef> dict = new Dictionary<string, SymbolRef>();

        public Scope(Scope parent)
        {
            this.parentScope = parent;
        }

        public Scope() : this(null) { }

        public SymbolRef this[string name]
        {
            get
            {
                SymbolRef ret;
                if (!dict.TryGetValue(name, out ret))
                {
                    if (parentScope == null)
                    {
                        return null;
                    }
                    else
                    {
                        return parentScope[name];
                    }
                }
                else
                {
                    return ret;
                }
            }
        }

        public void AddSymbol(SymbolRef s)
        {
            dict.Add(s.Name, s);
        }

        public bool RemoveSymbol(SymbolRef s)
        {
            return dict.Remove(s.Name);
        }

        #region IEnumerable<SymbolRef> Members

        public IEnumerator<SymbolRef> GetEnumerator()
        {
            return dict.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
