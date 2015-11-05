﻿using System;
using System.Collections.Generic;

namespace ContentCreator.StaticData
{
    public static class VehicleData
    {
        public static Dictionary<string, Tuple<string, uint>[]> Database = new Dictionary<string, Tuple<string, uint>[]>
        {
            {"Land", new []
            {
               new Tuple<string, uint>("Banshee", 0xC1E908D2),
               new Tuple<string, uint>("Alpha", 0x2DB8D1AA), 
            }},
            {"Motorcycles", new []
            {
                new Tuple<string, uint>("Bmx", 0x43779C54), 
                new Tuple<string, uint>("Bati", 0xF9300CC5),
            }},
        };
    }
}