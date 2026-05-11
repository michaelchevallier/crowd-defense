// Shim required for C# 9+ records on .NET Standard 2.1 (Unity runtime).
// See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init#metadata-encoding
#if !NETCOREAPP3_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER || NETSTANDARD2_1
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
