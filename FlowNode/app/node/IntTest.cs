﻿using FlowNode.node.Attribute;
using FlowNode.node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.app.node
{
    [Node]
    public class IntTest
    {
        [Function("", true)]
        public static void IntOutput(out int result)
        {
            result = 1;
        }

        [Function("", true)]
        public static void FloatOutPut( out float result)
        {
            result =1.0f;
        }

        [Function("", true)]
        public static void StringOutPut(out string result)
        {
            result ="hello";
        }

        [Function("", true)]
        public static void StringOutPut2(out string result)
        {
            result = "world";
        }
        [Function("", false)]
        public static void showMessage(object message)
        {
           Console.WriteLine(message!=null? message.ToString():"null");
        }
    }
}
