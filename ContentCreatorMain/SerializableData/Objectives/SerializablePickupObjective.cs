﻿using Rage;

namespace ContentCreator.SerializableData.Objectives
{
    public class SerializablePickupObjective : SerializableObjective
    {
        public bool Respawn { get; set; }
        public int Ammo { get; set; }
        public uint ModelHash { get; set; }
        public uint PickupHash { get; set; }

        private Object _veh;

        public virtual void SetObject(Object veh)
        {
            _veh = veh;
        }

        public virtual Object GetObject()
        {
            return _veh;
        }
    }
}