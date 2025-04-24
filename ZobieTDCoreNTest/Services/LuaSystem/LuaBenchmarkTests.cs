using System;
using System.Collections.Generic;
using System.Diagnostics;
using MoonSharp.Interpreter;
using NUnit.Framework;

namespace LuaVsCSharpBenchmark
{
    [TestFixture]
    public class LuaBenchmarkTests
    {
        private Script _script;
        private Stopwatch _sw;

        [SetUp]
        public void Setup()
        {
            _script = new Script();
            _sw = new Stopwatch();
        }

        [Test]
        public void Test_CallLuaFunctionVsCSharp()
        {
            _script.DoString("function sum(a, b) return a + b end");
            var luaFunc = _script.Globals.Get("sum");
            Console.WriteLine("Test_CallLuaFunctionVsCSharp");

            _sw.Restart();
            for (int i = 0; i < 1_000_000; i++)
                _script.Call(luaFunc, DynValue.NewNumber(3), DynValue.NewNumber(5));
            _sw.Stop();
            Console.WriteLine("Lua call: " + _sw.ElapsedMilliseconds + " ms");

            _sw.Restart();
            for (int i = 0; i < 1_000_000; i++)
                _ = 3 + 5;
            _sw.Stop();
            Console.WriteLine("C# call: " + _sw.ElapsedMilliseconds + " ms");
        }

        [Test]
        public void Test_LuaTableVsCSharpDictionary()
        {
            Console.WriteLine("Test_LuaTableVsCSharpDictionary");
            _script.DoString("data = {x = 10, y = 20}");

            _sw.Restart();
            for (int i = 0; i < 1_000_000; i++)
            {
                var x = _script.Globals.Get("data").Table.Get("x").Number;
                var y = _script.Globals.Get("data").Table.Get("y").Number;
            }
            _sw.Stop();
            Console.WriteLine("Lua table access: " + _sw.ElapsedMilliseconds + " ms");

            var dict = new Dictionary<string, double> { ["x"] = 10, ["y"] = 20 };
            _sw.Restart();
            for (int i = 0; i < 1_000_000; i++)
            {
                var x = dict["x"];
                var y = dict["y"];
            }
            _sw.Stop();
            Console.WriteLine("C# dictionary access: " + _sw.ElapsedMilliseconds + " ms");
        }

        [Test]
        public void Test_LuaLoopVsCSharpLoop()
        {
            Console.WriteLine("Test_LuaLoopVsCSharpLoop");
            _script.DoString(@"
                function loop()
                    local count = 0
                    for i = 1, 1000000 do
                        count = count + 1
                    end
                    return count
                end");

            _sw.Restart();
            _script.Call(_script.Globals.Get("loop"));
            _sw.Stop();
            Console.WriteLine("Lua loop: " + _sw.ElapsedMilliseconds + " ms");

            _sw.Restart();
            int count = 0;
            for (int i = 0; i < 1_000_000; i++) count++;
            _sw.Stop();
            Console.WriteLine("C# loop: " + _sw.ElapsedMilliseconds + " ms");
        }

        [Test]
        public void Test_LuaRecursiveFibVsCSharp()
        {
            Console.WriteLine("Test_LuaRecursiveFibVsCSharp");
            _script.DoString(@"
                function fib(n)
                    if n < 2 then return n
                    else return fib(n-1) + fib(n-2)
                    end
                end");

            _sw.Restart();
            _script.Call(_script.Globals.Get("fib"), DynValue.NewNumber(20));
            _sw.Stop();
            Console.WriteLine("Lua fib(20): " + _sw.ElapsedMilliseconds + " ms");

            int Fib(int n) => (n < 2) ? n : Fib(n - 1) + Fib(n - 2);
            _sw.Restart();
            Fib(20);
            _sw.Stop();
            Console.WriteLine("C# fib(20): " + _sw.ElapsedMilliseconds + " ms");
        }

        [Test]
        public void Test_LuaCallsCSharpCallback()
        {
            Console.WriteLine("Test_LuaCallsCSharpCallback");

            DynValue callback = DynValue.NewCallback((ctx, args) =>
            {
                return DynValue.NewString("OK");
            });

            _script.Globals["csfunc"] = callback;

            _sw.Restart();
            _script.DoString(@"
                for i = 1, 1000000 do
                    csfunc()
                end
            ");
            _sw.Stop();
            Console.WriteLine("Lua -> C# callback: " + _sw.ElapsedMilliseconds + " ms");
        }

        [Test]
        public void Test_LoadLuaScriptFromFile()
        {
            Console.WriteLine("Test_LoadLuaScriptFromFile");

            // Tạo file script Lua tạm thời
            string filePath = "temp_script.lua";
            System.IO.File.WriteAllText(filePath, @"
        function calculate()
            local total = 0
            for i = 1, 100000 do
                total = total + i
            end
            return total
        end
    ");

            // Đọc nội dung từ file và thực thi
            string code = System.IO.File.ReadAllText(filePath);
            _sw.Restart();
            _script.DoString(code);
            _script.Call(_script.Globals.Get("calculate"));
            _sw.Stop();

            Console.WriteLine("Load & execute Lua file: " + _sw.ElapsedMilliseconds + " ms");

            // Xoá file tạm sau khi test
            System.IO.File.Delete(filePath);
        }

    }
}
