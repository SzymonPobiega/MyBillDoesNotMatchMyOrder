﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Messages;

public class OrderLine
{
    public string Id { get; set; }
    [Range((int)Filling.Meat, (int)Filling.Ruskie, ErrorMessage = "Invalid filling.")]
    public Filling Filling { get; set; }
    public int Quantity { get; set; }
    public Order Order { get; set; }
}