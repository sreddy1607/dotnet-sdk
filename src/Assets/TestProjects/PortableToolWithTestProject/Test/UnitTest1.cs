using System;
using consoledemo;
using Xunit;

namespace XUnitTestProject1
{
    public class UnitTest1
    {
        [Fact]
        public void call_main_should_not_throw()
        {
            Program.Main(Array.Empty<string>());
        }
    }
}
