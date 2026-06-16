using Microsoft.AspNetCore.Identity;
using System;

public class Program
{
    public static void Main()
    {
        var hasher = new PasswordHasher<object>();
        var hash = hasher.HashPassword(null, ""Password123!"");
        Console.WriteLine(hash);
    }
}
