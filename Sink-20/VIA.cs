namespace Sink_20
{
   public class VIA : Chip
   {
      private int mPortB;
      private int mPortA;
      private int mDataDirectionB;
      private int mDataDirectionA;

      private int mTimer1 = 0xffff;
      private int mTimer1Latch = 0xffff;

      private int mTimer2 = 0xffff;
      private int mTimer2Latch = 0xffff;

      private int mAuxiliaryControlRegister = 0x40;
      private int mInterruptFlagRegister;
      private int mInterruptEnableRegister;

      private long mLastCycles;

      private Keyboard mKeyboard;
      private int mStartAddress;

      public void Init(CPU cpu, Keyboard keyb, int startAddress)
      {
         mCpu = cpu;
         mKeyboard = keyb;
         mStartAddress = startAddress;
      }

      public override bool isAddrInRange(int address)
      {
         return address >= mStartAddress && address < mStartAddress + 16;
      }

      private void updateIRQ()
      {
         bool interruptOn = ((mInterruptFlagRegister & mInterruptEnableRegister & 0x7f) > 0);
         mCpu.setIRQLow(interruptOn);
      }

      public override int performRead(int address, long cycles)
      {
         int localAddress = address - mStartAddress;
         switch (localAddress)
         {
            case 0:
               // port B
               if (mStartAddress == 0x9120)
               {
                  return mKeyboard.readStick(mPortB, mDataDirectionB);
               }
               break;
            case 1:
            case 15:
               // port A
               if (mStartAddress == 0x9110)
               {
                  return mKeyboard.readStick(mPortA, mDataDirectionA);
               }
               if (mStartAddress == 0x9120)
               {
                  return mKeyboard.read(mPortB, mDataDirectionA);
               }
               break;
            case 2:
               return mDataDirectionB;
            case 3:
               return mDataDirectionA;
            case 4:
               // T1 low
               // now clear the interrupt flag
               mInterruptFlagRegister &= ~0x40;
               updateIRQ();
               return mTimer1 & 0xff;
            case 5:
               // T1 hi
               return mTimer1 >> 8;
            case 6:
               return mTimer1Latch & 0xff;
            case 7:
               return mTimer1Latch >> 8;
            case 8:
               // T2 low
               // now clear the interrupt flag
               mInterruptFlagRegister &= ~0x20;
               updateIRQ();
               return mTimer2 & 0xff;
            case 9:
               return mTimer2 >> 8;
            case 0x0b:
               return mAuxiliaryControlRegister;
            case 0x0d:
               if ((mInterruptFlagRegister & mInterruptEnableRegister & 0x60) > 0)
               {
                  return mInterruptFlagRegister | 0x80;
               }
               return mInterruptFlagRegister;
            case 0x0e:
               return mInterruptEnableRegister | 0x80;
         }
         return 0;
      }

      public override void performWrite(int address, int data, long cycles)
      {
         int localAddress = address - mStartAddress;
         switch (localAddress)
         {
            case 0:
               // port B
               mPortB = data;
               break;
            case 1:
            case 15:
               // port A with/without handshake
               mPortA = data;
               break;
            case 2:
               // DDB
               mDataDirectionB = data;
               break;
            case 3:
               // DDA
               mDataDirectionA = data;
               break;
            case 4:
               // T1 low
               mTimer1Latch &= 0xff00;
               mTimer1Latch |= data;
               break;
            case 5:
               // T1 hi
               mTimer1Latch &= 0xff;
               mTimer1Latch |= data << 8;
               mTimer1 = mTimer1Latch;
               // now clear the interrupt flag
               mInterruptFlagRegister &= ~0x40;
               updateIRQ();
               break;
            case 6:
               mTimer1Latch &= 0xff00;
               mTimer1Latch |= data;
               break;
            case 7:
               mTimer1Latch &= 0xff;
               mTimer1Latch |= data << 8;
               mTimer1 = 0;
               break;
            case 8:
               // T2 low
               mTimer2Latch &= 0xff00;
               mTimer2Latch |= data;
               break;
            case 9:
               // T2 hi
               mTimer2Latch &= 0xff;
               mTimer2Latch |= data << 8;
               mTimer2 = mTimer2Latch;
               // now clear the interrupt flag
               mInterruptFlagRegister &= ~0x20;
               updateIRQ();
               break;
            case 0x0b:
               mAuxiliaryControlRegister = data;
               break;
            case 0x0d:
               mInterruptFlagRegister &= (~data);
               updateIRQ();
               break;
            case 0x0e:
               if ((data & 0x80) > 0)
               {
                  mInterruptEnableRegister |= (data & 0x7f);
               }
               else
               {
                  mInterruptEnableRegister &= ~data;
               }
               break;
         }
      }

      public void Update(long cycles)
      {
         int cyclesElapsed = (int)(cycles - mLastCycles);
         mLastCycles = cycles;

         if (mTimer1 > 0)
         {
            mTimer1 -= cyclesElapsed;
            if (mTimer1 <= 0)
            {
               mTimer1 = 0;
               if ((mAuxiliaryControlRegister & 0x40) > 0)
               {
                  mTimer1 = mTimer1Latch;
               }

               if ((mInterruptEnableRegister & 0x40) > 0)
               {
                  // set IRQ
                  mInterruptFlagRegister |= 0xc0;
                  mCpu.setIRQLow(true);
               }
            }
         }

         if ((mAuxiliaryControlRegister & 0x20) == 0)
         {
            if (mTimer2 > 0)
            {
               mTimer2 -= cyclesElapsed;
               if (mTimer2 <= 0)
               {
                  mTimer2 = 0;
                  if ((mInterruptEnableRegister & 0x20) > 0)
                  {
                     mInterruptFlagRegister |= 0xa0;
                     mCpu.setIRQLow(true);
                  }
               }
            }
         }
      }
   }
}
