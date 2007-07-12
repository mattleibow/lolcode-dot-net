using System;
using System.Collections.Generic;
using System.Text;

namespace stdlol
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public class LOLCodeFunctionAttribute : Attribute
    {
        private string m_Name;
        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        public LOLCodeFunctionAttribute(string name)
        {
            m_Name = name;
        }

        public LOLCodeFunctionAttribute()
        {
            m_Name = null;
        }
    }
}
