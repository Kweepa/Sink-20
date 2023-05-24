using System.Collections.Generic;

namespace Sink_20
{
   public class Memory
   {
      int[] mMemory = new int[65536];
      List<Chip> mChips = new List<Chip>();
      public bool[] hasExpansion = new bool[5];

      public void AddChip(Chip chip)
      {
         mChips.Add(chip);
      }

      public void Fill(int addr, byte[] bytes, int offset)
      {
         for (int i = 0; i < bytes.Length - offset; ++i)
         {
            mMemory[addr + i] = bytes[i + offset];
         }
      }

      public void Poke(int addr, int val)
      {
         mMemory[addr] = val;
      }

      public void Poke16(int addr, int val)
      {
         mMemory[addr] = val & 0xff;
         mMemory[addr + 1] = val >> 8;
      }

      public int Peek(int addr)
      {
         return mMemory[addr];
      }

      public int Peek16(int addr)
      {
         return mMemory[addr] + (mMemory[addr + 1] << 8);
      }

      public int GetLoadAddress()
      {
         if ( hasExpansion[1] ) // first 8k
         {
            return 0x1200;
         }
         else if ( hasExpansion[0] ) // 3k
         {
            return 0x0400;
         }
         else
         {
            return 0x1000;
         }
      }

      public int read(int addr)
      {
         foreach (Chip chip in mChips)
         {
            if (chip.isAddrInRange(addr))
            {
               return chip.performRead(addr, 1);
            }
         }

         return mMemory[addr];
      }

      public void write(int addr, int data)
      {
         bool consumed = false;
         foreach (Chip chip in mChips)
         {
            if (chip.isAddrInRange(addr))
            {
               chip.performWrite(addr, data, 0);
               consumed = true;
               break;
            }
         }
         if (!consumed)
         {
            int page = addr>>8;
            // don't write to ROM!
            if (page < 0x04 || (page >= 0x10 && page < 0x20) || (page >= 0x94 && page < 0x98))
            {
               mMemory[addr] = data;
            }
            else if ( hasExpansion[0] && page >= 0x04 && page < 0x10 )
            {
               mMemory[addr] = data;
            }
            else
            {
               page >>= 5;
               switch ( page )
               {
               case 1:
                  if ( hasExpansion[1] )
                  {
                     mMemory[addr] = data;
                  }
                  break;
               case 2:
                  if ( hasExpansion[2] )
                  {
                     mMemory[addr] = data;
                  }
                  break;
               case 3:
                  if ( hasExpansion[3] )
                  {
                     mMemory[addr] = data;
                  }
                  break;
               case 5:
                  if ( hasExpansion[4] )
                  {
                     mMemory[addr] = data;
                  }
                  break;
               }
            }
         }
      }
   }
}
