﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmaServerManager
{
    [Serializable]
    public class Arma3ClassObject
    {
        public string ClassName;
        public List<Arma3ClassObject> SubClasses = new List<Arma3ClassObject>();
        public List<SrvParam> ClassMembers = new List<SrvParam>();


        public Arma3ClassObject(string name)
        {
            ClassName = name;
        }

        public void RemoveSubclassesByName(string name)
        {
            this.SubClasses.RemoveAll(x => x.ClassName == name);
        }
    }
}
