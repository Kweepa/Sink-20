namespace Sink_20
{
   public abstract class Chip
   {
      // C64 specific names - but... basically just numbers
      public static readonly int VIA_TIMER_IRQ = 2;
      public static readonly int KEYBOARD_NMI = 1;

      // Interrupt management
      // As soon as either of these are != 0 then nmi/irq low!
      private int nmiFlags = 0;
      private int oldNmiFlags = 0;
      private int irqFlags = 0;
      private int oldIrqFlags = 0;

      protected CPU mCpu;

      public int getNMIFlags()
      {
         return nmiFlags;
      }

      public int getIRQFlags()
      {
         return irqFlags;
      }

      public bool setIRQ(int irq)
      {
         bool val = (irqFlags & irq) == 0;
         irqFlags |= irq;
         if (irqFlags != oldIrqFlags)
         {
            mCpu.setIRQLow(irqFlags != 0);
            oldIrqFlags = irqFlags;
         }
         return val;
      }

      public void clearIRQ(int irq)
      {
         irqFlags &= ~irq;
         if (irqFlags != oldIrqFlags)
         {
            mCpu.setIRQLow(irqFlags != 0);
            oldIrqFlags = irqFlags;
         }
      }

      public bool setNMI(int nmi)
      {
         bool val = (nmiFlags & nmi) == 0;
         nmiFlags |= nmi;
         if (nmiFlags != oldNmiFlags)
         {
            mCpu.setNMILow(nmiFlags != 0);
            oldNmiFlags = nmiFlags;
         }
         return val;
      }


      public void clearNMI(int nmi)
      {
         nmiFlags &= ~nmi;
         if (nmiFlags != oldNmiFlags)
         {
            mCpu.setNMILow(nmiFlags != 0);
            oldNmiFlags = nmiFlags;
         }
      }

      public abstract int performRead(int address, long cycles);
      public abstract void performWrite(int address, int data, long cycles);

      public abstract bool isAddrInRange(int addr);
   }
}
