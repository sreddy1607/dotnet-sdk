﻿using System;
using Newtonsoft.Json;

namespace MyTestProject
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            string result = JsonConvert.SerializeObject(new { a = "test" });
        }
    }
}
