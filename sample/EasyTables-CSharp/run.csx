﻿using System;

public static void Run(string input, out Item newItem)
{
    newItem = new Item
    {
        Text = "Hello from C#! " + input
    };    
}

public class Item
{
    public string Id { get; set; }
    public string Text { get; set; }
    public bool IsProcessed { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }

    // EasyTable properties
    public DateTimeOffset CreatedAt { get; set; }
}