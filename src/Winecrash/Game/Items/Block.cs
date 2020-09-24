﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WEngine;

namespace Winecrash
{
    public class Block : Item
    {
        public bool Transparent { get; set; } = false;

        public double DigTime { get; set; } = 1.0D;

        public void Tick(TickType type, Chunk chunk, Vector3I position) 
        {
            switch(type)
            {
                case TickType.World:
                    WorldTick(chunk, position);
                    break;

                case TickType.Block:
                    BlockTick(chunk, position);
                    break;
            }
        }

        protected virtual void WorldTick(Chunk chunk, Vector3I position) { }
        protected virtual void BlockTick(Chunk chunk, Vector3I position) { }

        public static void PlayerTickNeighbors(Vector3I globalBlockPosition)
        {
            throw new NotImplementedException("The player neighbors tick hasn't been implemented yet.");
        }

        public static void PlayerTickNeighbors(Chunk chunk, Vector3I position)
        {
            throw new NotImplementedException("The player neighbors tick hasn't been implemented yet.");
        }
    }
}