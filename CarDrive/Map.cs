﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;

namespace CarDrive
{
    public class Map
    {
        public String Name { get; }
        public Point StartPoint { get; set; }
        public List<Polyline> Obstacles { get; set; }

        public Map(string name)
        {
            Name = name;
        }
    }
}