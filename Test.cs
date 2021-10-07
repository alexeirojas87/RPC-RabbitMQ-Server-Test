using RabbitMQ_Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ_Server
{
    public class Test
    {
        public int MyProperty { get; set; }
        public Test() { }

        public Test(int property)
        {
            MyProperty = property;
        }
        public string TestMethod()
        {
            return "hello";
        }
        
        public RPCObjectReturn TestMethodObject()
        {
            return new RPCObjectReturn() { MyProperty = 1 };
        }
        public RPCObjectReturn TestMethodObjectwithParam(int val)
        {
            return new RPCObjectReturn() { MyProperty = val };
        }
    }
}
