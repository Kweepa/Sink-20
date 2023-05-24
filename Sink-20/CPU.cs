using System;

namespace Sink_20
{
   public class CPU
   {
      public void Init(Memory _mem)
      {
         mem = _mem;
         SoftReset();
      }

      public void SoftReset()
      {
         pc = mem.Peek16( 0xfffc );
         sp = 0x1fd;
         a = x = y = 0;
         carry = false;
         zero = false;
         idis = false;
         bcd = false;
         brk = false;
         oflow = false;
         neg = false;
      }

      Memory mem;

      public int pc, sp;
      int a, x, y;
      bool carry, zero, idis, bcd, brk, oflow, neg;

      void UpdateN(int v)
      {
         neg = (v < 0 || v > 127);
      }

      void UpdateV(int v)
      {
         oflow = (v < 0 || v > 255);
      }

      void UpdateZ(int v)
      {
         zero = (v == 0);
      }

      int GetImm()
      {
         return mem.read(pc++);
      }

      int GetZP()
      {
         return mem.read(AddrZP());
      }

      int AddrZP()
      {
         return mem.read(pc++);
      }

      int GetZP_X()
      {
         return mem.read(AddrZP_X());
      }

      int AddrZP_X()
      {
         return (mem.read(pc++) + x) & 255;
      }

      int GetZP_Y()
      {
         return mem.read(AddrZP_Y());
      }

      int AddrZP_Y()
      {
         return (mem.read(pc++) + y) & 255;
      }

      int GetAbsolute()
      {
         return mem.read(AddrAbsolute());
      }

      int AddrAbsolute()
      {
         return (mem.read(pc++) + (mem.read(pc++) << 8)) & 65535;
      }

      int GetAbsolute_X()
      {
         return mem.read(AddrAbsolute_X());
      }

      int AddrAbsolute_X()
      {
         return (mem.read(pc++) + (mem.read(pc++) << 8) + x) & 65535;
      }

      int GetAbsolute_Y()
      {
         return mem.read(AddrAbsolute_Y());
      }

      int AddrAbsolute_Y()
      {
         return (mem.read(pc++) + (mem.read(pc++) << 8) + y) & 65535;
      }

      int GetIndirect_X()
      {
         return mem.read(AddrIndirect_X());
      }

      int AddrIndirect_X()
      {
         int addr = (mem.read(pc++) + x) & 0xff;
         return mem.read(addr) + (mem.read(addr + 1) << 8);
      }

      int GetIndirect_Y()
      {
         return mem.read(AddrIndirect_Y());
      }

      int AddrIndirect_Y()
      {
         int addr = mem.read(pc++);
         return (mem.read(addr) + (mem.read(addr + 1) << 8) + y) & 65535;
      }

      int PackSR()
      {
         return (carry ? 1 : 0) | (zero ? 2 : 0) | (idis ? 4 : 0) | (bcd ? 8 : 0) | (brk ? 16 : 0) | 32 | (oflow ? 64 : 0) | (neg ? 128 : 0);
      }

      void UnpackSR(int s)
      {
         carry = (s & 1) > 0;
         zero = (s & 2) > 0;
         idis = (s & 4) > 0;
         bcd = (s & 8) > 0;
         brk = (s & 16) > 0;
         oflow = (s & 64) > 0;
         neg = (s & 128) > 0;
      }

      void ADC(int m)
      {
         a = a + m + (carry ? 1 : 0);
         carry = a > 255;
         UpdateV(a);
         a &= 255;
         UpdateN(a);
         UpdateZ(a);
      }

      void AND(int m)
      {
         a = a & m;
         UpdateN(a);
         UpdateZ(a);
      }

      void ASL()
      {
         a <<= 1;
         carry = a > 255;
         a &= 255;
         UpdateN(a);
         UpdateZ(a);
      }

      void ASL(int m)
      {
         int v = mem.read(m) << 1;
         carry = v > 255;
         v &= 255;
         UpdateN(v);
         UpdateZ(v);
         mem.write(m, v);
      }

      void JumpCond(bool cond)
      {
         int off = mem.read(pc++);
         if (cond)
         {
            if (off > 127) off -= 256;
            pc += off;
         }
      }

      void BIT(int m)
      {
         neg = (m & 128) > 0;
         oflow = (m & 64) > 0;
         zero = (a & m) == 0;
      }

      void DumpCock()
      {
         for (int i = 0; i < instructionCache.Length; ++i)
         {
            System.Console.WriteLine(instructionCache[(i + lastInstruction) % instructionCache.Length]);
         }
      }

      void BRK()
      {
         brk = true;
         checkInterrupt = true;
      }

      void CMP(int m)
      {
         int v = a - m;
         UpdateN(v);
         UpdateZ(v & 255);
         carry = a >= m;
      }

      void CPX(int m)
      {
         int v = x - m;
         UpdateN(v);
         UpdateZ(v & 255);
         carry = x >= m;
      }

      void CPY(int m)
      {
         int v = y - m;
         UpdateN(v);
         UpdateZ(v & 255);
         carry = y >= m;
      }

      void DEC(int m)
      {
         int v = mem.read(m) - 1;
         v &= 255;
         UpdateN(v);
         UpdateZ(v);
         mem.write(m, v);
      }

      void DEX()
      {
         --x;
         x &= 255;
         UpdateN(x);
         UpdateZ(x);
      }

      void DEY()
      {
         --y;
         y &= 255;
         UpdateN(y);
         UpdateZ(y);
      }

      void EOR(int m)
      {
         a = a ^ m;
         UpdateN(a);
         UpdateZ(a);
      }

      void INC(int m)
      {
         int v = mem.read(m) + 1;
         v &= 255;
         UpdateN(v);
         UpdateZ(v);
         mem.write(m, v);
      }

      void INX()
      {
         ++x;
         x &= 255;
         UpdateN(x);
         UpdateZ(x);
      }

      void INY()
      {
         ++y;
         y &= 255;
         UpdateN(y);
         UpdateZ(y);
      }

      void JMP(int m)
      {
         pc = mem.read(m) + (mem.read(m + 1) << 8);
      }

      void JSR()
      {
         int newpc = AddrAbsolute();
         --pc;
         mem.write(sp--, pc >> 8);
         mem.write(sp--, pc & 255);
         pc = newpc;
      }

      void LDA(int m)
      {
         a = m;
         UpdateN(a);
         UpdateZ(a);
      }

      void LDX(int m)
      {
         x = m;
         UpdateN(x);
         UpdateZ(x);
      }

      void LDY(int m)
      {
         y = m;
         UpdateN(y);
         UpdateZ(y);
      }

      void LSR()
      {
         carry = (a & 1) > 0;
         a >>= 1;
         UpdateN(a);
         UpdateZ(a);
      }

      void LSR(int m)
      {
         int v = mem.read(m);
         carry = (v & 1) > 0;
         v >>= 1;
         UpdateN(v);
         UpdateZ(v);
         mem.write(m, v);
      }

      void ORA(int m)
      {
         a |= m;
         UpdateN(a);
         UpdateZ(a);
      }

      void PHA()
      {
         mem.write(sp--, a);
      }

      void PHP()
      {
         mem.write(sp--, PackSR());
      }

      void PLA()
      {
         a = mem.read(++sp);
         UpdateN(a);
         UpdateZ(a);
      }

      void PLP()
      {
         UnpackSR(mem.read(++sp));
      }

      void ROL()
      {
         a <<= 1;
         a += (carry ? 1 : 0);
         carry = a > 255;
         a &= 255;
         UpdateN(a);
         UpdateZ(a);
      }

      void ROL(int m)
      {
         int v = mem.read(m);
         v <<= 1;
         v += (carry ? 1 : 0);
         carry = v > 255;
         v &= 255;
         UpdateN(v);
         UpdateZ(v);
         mem.write(m, v);
      }

      void ROR()
      {
         bool newCarry = (a & 1) > 0;
         a >>= 1;
         a += (carry ? 128 : 0);
         UpdateN(a);
         UpdateZ(a);
         carry = newCarry;
      }

      void ROR(int m)
      {
         int v = mem.read(m);
         bool newCarry = (v & 1) > 0;
         v >>= 1;
         v += (carry ? 128 : 0);
         UpdateN(v);
         UpdateZ(v);
         carry = newCarry;
         mem.write(m, v);
      }

      void RTI()
      {
         UnpackSR(mem.read(++sp));
         pc = mem.read(++sp);
         pc += mem.read(++sp) << 8;
         brk = false;
         checkInterrupt = true;
      }

      void RTS()
      {
         pc = mem.read(++sp);
         pc += mem.read(++sp) << 8;
         ++pc;
      }

      void SBC(int m)
      {
         a = a - m - (carry ? 0 : 1);
         carry = a >= 0;
         UpdateV(a);
         a &= 255;
         UpdateN(a);
         UpdateZ(a);
      }

      void STA(int m)
      {
         mem.write(m, a);
      }

      void STX(int m)
      {
         mem.write(m, x);
      }

      void STY(int m)
      {
         mem.write(m, y);
      }

      void TAX()
      {
         x = a;
         UpdateN(x);
         UpdateZ(x);
      }

      void TAY()
      {
         y = a;
         UpdateN(y);
         UpdateZ(y);
      }

      void TSX()
      {
         x = sp & 0xff;
         UpdateN(x);
         UpdateZ(x);
      }

      void TXA()
      {
         a = x;
         UpdateN(a);
         UpdateZ(a);
      }

      void TXS()
      {
         sp = 0x100 + x;
      }

      void TYA()
      {
         a = y;
         UpdateN(a);
         UpdateZ(a);
      }

      string[] mne = new string[]
      {
         // 0x00
         "brk", "ora", " - ", " - ", " - ", "ora", "asl", " - ",
         "php", "ora", "asl", " - ", " - ", "ora", "asl", " - ",
         "bpl", "ora", " - ", " - ", " - ", "ora", "asl", " - ",
         "clc", "ora", " - ", " - ", " - ", "ora", "asl", " - ",

         // 0x20
         "jsr", "and", " - ", " - ", "bit", "and", "rol", " - ",
         "plp", "and", "rol", " - ", "bit", "and", "rol", " - ",
         "bmi", "and", " - ", " - ", " - ", "and", "rol", " - ",
         "sec", "and", " - ", " - ", " - ", "and", "rol", " - ",
         
         // 0x40
         "rti", "eor", " - ", " - ", " - ", "eor", "lsr", " - ",
         "pha", "eor", "lsr", " - ", "jmp", "eor", "lsr", " - ",
         "bvc", "eor", " - ", " - ", " - ", "eor", "lsr", " - ",
         "cli", "eor", " - ", " - ", " - ", "eor", "lsr", " - ",

         // 0x60
         "rts", "adc", " - ", " - ", " - ", "adc", "ror", " - ",
         "pla", "adc", "ror", " - ", "jmp", "adc", "ror", " - ",
         "bvs", "adc", " - ", " - ", " - ", "adc", "ror", " - ",
         "sei", "adc", " - ", " - ", " - ", "adc", "ror", " - ",

         // 0x80
         " - ", "sta", " - ", " - ", "sty", "sta", "stx", " - ",
         "dey", " - ", "txa", " - ", "sty", "sta", "stx", " - ",
         "bcc", "sta", " - ", " - ", "sty", "sta", "stx", " - ",
         "tya", "sta", "txs", " - ", " - ", "sta", " - ", " - ",

         // 0xa0
         "ldy", "lda", "ldx", " - ", "ldy", "lda", "ldx", " - ",
         "tay", "lda", "tax", " - ", "ldy", "lda", "ldx", " - ",
         "bcs", "lda", " - ", " - ", "ldy", "lda", "ldx", " - ",
         "clv", "lda", "tsx", " - ", "ldy", "lda", "ldx", " - ",

         // 0xc0
         "cpy", "cmp", " - ", " - ", "cpy", "cmp", "dec", " - ",
         "iny", "cmp", "dex", " - ", "cpy", "cmp", "dec", " - ",
         "bne", "cmp", " - ", " - ", " - ", "cmp", "dec", " - ",
         "cld", "cmp", " - ", " - ", " - ", "cmp", "dec", " - ",

         // 0xe0
         "cpx", "sbc", " - ", " - ", "cpx", "sbc", "inc", " - ",
         "inx", "sbc", "nop", " - ", "cpx", "sbc", "inc", " - ",
         "beq", "sbc", " - ", " - ", " - ", "sbc", "inc", " - ",
         "sed", "sbc", " - ", " - ", " - ", "sbc", "inc", " - ",
      };

      enum EAddrMode
      {
         None,
         Imm,
         ZP,
         ZPX,
         ZPY,
         Abs,
         AbsX,
         AbsY,
         Ind,
         IndX,
         IndY,
         Rel
      }

      EAddrMode[] addrModes = new EAddrMode[]
      {
         EAddrMode.None, EAddrMode.IndX, EAddrMode.Imm,  EAddrMode.None, EAddrMode.ZP,   EAddrMode.ZP,   EAddrMode.ZP,   EAddrMode.None,
         EAddrMode.None, EAddrMode.Imm,  EAddrMode.None, EAddrMode.None, EAddrMode.Abs,  EAddrMode.Abs,  EAddrMode.Abs,  EAddrMode.None,
         EAddrMode.Rel,  EAddrMode.IndY, EAddrMode.None, EAddrMode.None, EAddrMode.ZPX,  EAddrMode.ZPX,  EAddrMode.ZPX,  EAddrMode.None,
         EAddrMode.None, EAddrMode.AbsY, EAddrMode.None, EAddrMode.None, EAddrMode.AbsX, EAddrMode.AbsX, EAddrMode.AbsX, EAddrMode.None
      };

      string GetArgs(int cmd)
      {
         EAddrMode mode = addrModes[cmd & 0x1f];
         if ((cmd & 0x9f) == 0x80) mode = EAddrMode.Imm; 
         // special cases
         switch (cmd)
         {
            case 0x20:
               mode = EAddrMode.Abs;
               break;
            case 0x6c:
               mode = EAddrMode.Ind;
               break;
            case 0x96:
            case 0xb6:
               mode = EAddrMode.ZPY;
               break;
            case 0xbe:
               mode = EAddrMode.AbsY;
               break;
         }
         switch (mode)
         {
         case EAddrMode.Imm:
            return String.Format(" #${0:x2}", mem.Peek(pc));
         case EAddrMode.ZP:
            return String.Format(" ${0:x2}", mem.Peek(pc));
         case EAddrMode.ZPX:
            return String.Format(" ${0:x2},x", mem.Peek(pc));
         case EAddrMode.ZPY:
            return String.Format(" ${0:x2},y", mem.Peek(pc));
         case EAddrMode.Abs:
            return String.Format(" ${0:x4}", mem.Peek16(pc));
         case EAddrMode.AbsX:
            return String.Format(" ${0:x4},x", mem.Peek16(pc));
         case EAddrMode.AbsY:
            return String.Format(" ${0:x4},y", mem.Peek16(pc));
         case EAddrMode.Ind:
            return String.Format(" (${0:x4})", mem.Peek16(pc));
         case EAddrMode.IndX:
            return String.Format(" (${0:x2},x)", mem.Peek(pc));
         case EAddrMode.IndY:
            return String.Format(" (${0:x2}),y", mem.Peek(pc));
         case EAddrMode.Rel:
            {
               int off = mem.Peek(pc);
               int addr = pc + 1 + (off > 127 ? off - 256 : off);
               return String.Format(" ${0:x4}", addr);
            }
         }
         return "";
      }

      string[] instructionCache = new string[2048];
      int lastInstruction = 0;

      long[] time = new long[8192];

      public int Update()
      {
         if (CheckInterrupts())
         {
            return 7;
         }
         int curPc = pc;
         int cmd = mem.read(pc++);
#if false
         string instruction = String.Format(
            "A:{0:x2} X:{1:x2} Y:{2:x2} SP:{3:x3} {4}{5}{6}{7}{8}{9}",
            a, x, y, sp, neg ? "N" : "-", zero ? "Z" : "-", carry ? "C" : "-",
            idis ? "I" : "-", bcd ? "D" : "-", oflow ? "V" : "-");
         instruction += String.Format(" {0:x4} {1} {2}", pc - 1, mne[cmd], GetArgs(cmd));

         instructionCache[lastInstruction] = instruction;
         lastInstruction++;
         lastInstruction %= instructionCache.Length;
#endif
         int c = 2;
         switch (cmd)
         {
            // ADC
            case 0x69: c = 2; ADC(GetImm()); break;
            case 0x65: c = 3; ADC(GetZP()); break;
            case 0x75: c = 4; ADC(GetZP_X()); break;
            case 0x6d: c = 4; ADC(GetAbsolute()); break;
            case 0x7d: c = 4; ADC(GetAbsolute_X()); break;
            case 0x79: c = 4; ADC(GetAbsolute_Y()); break;
            case 0x61: c = 6; ADC(GetIndirect_X()); break;
            case 0x71: c = 5; ADC(GetIndirect_Y()); break;
            // AND
            case 0x29: c = 2; AND(GetImm()); break;
            case 0x25: c = 3; AND(GetZP()); break;
            case 0x35: c = 4; AND(GetZP_X()); break;
            case 0x2d: c = 4; AND(GetAbsolute()); break;
            case 0x3d: c = 4; AND(GetAbsolute_X()); break;
            case 0x39: c = 4; AND(GetAbsolute_Y()); break;
            case 0x21: c = 6; AND(GetIndirect_X()); break;
            case 0x31: c = 5; AND(GetIndirect_Y()); break;
            // ASL
            case 0x0a: c = 2; ASL(); break;
            case 0x06: c = 5; ASL(AddrZP()); break;
            case 0x16: c = 6; ASL(AddrZP_X()); break;
            case 0x0e: c = 6; ASL(AddrAbsolute()); break;
            case 0x1e: c = 7; ASL(AddrAbsolute_X()); break;
            // BCC
            case 0x90: c = 2; JumpCond(!carry); break;
            // BCS
            case 0xb0: c = 2; JumpCond(carry); break;
            // BEQ
            case 0xf0: c = 2; JumpCond(zero); break;
            // BIT
            case 0x24: c = 3; BIT(GetZP()); break;
            case 0x2c: c = 4; BIT(GetAbsolute()); break;
            // BMI
            case 0x30: c = 2; JumpCond(neg); break;
            // BNE
            case 0xd0: c = 2; JumpCond(!zero); break;
            // BPL
            case 0x10: c = 2; JumpCond(!neg); break;
            // BRK
            case 0x00: c = 7; BRK(); break;
            // BVC
            case 0x50: c = 2; JumpCond(!oflow); break;
            // BVS
            case 0x70: c = 2; JumpCond(oflow); break;
            // CLC
            case 0x18: c = 2; carry = false; break;
            // CLD
            case 0xd8: c = 2; bcd = false; break;
            // CLI
            case 0x58: c = 2; idis = false; break;
            // CLV
            case 0xb8: c = 2; oflow = false; break;
            // CMP
            case 0xc9: c = 2; CMP(GetImm()); break;
            case 0xc5: c = 3; CMP(GetZP()); break;
            case 0xd5: c = 4; CMP(GetZP_X()); break;
            case 0xcd: c = 4; CMP(GetAbsolute()); break;
            case 0xdd: c = 4; CMP(GetAbsolute_X()); break;
            case 0xd9: c = 4; CMP(GetAbsolute_Y()); break;
            case 0xc1: c = 6; CMP(GetIndirect_X()); break;
            case 0xd1: c = 5; CMP(GetIndirect_Y()); break;
            // CPX
            case 0xe0: c = 2; CPX(GetImm()); break;
            case 0xe4: c = 3; CPX(GetZP()); break;
            case 0xec: c = 4; CPX(GetAbsolute()); break;
            // CPY
            case 0xc0: c = 2; CPY(GetImm()); break;
            case 0xc4: c = 3; CPY(GetZP()); break;
            case 0xcc: c = 4; CPY(GetAbsolute()); break;
            // DEC
            case 0xc6: c = 5; DEC(AddrZP()); break;
            case 0xd6: c = 6; DEC(AddrZP_X()); break;
            case 0xce: c = 6; DEC(AddrAbsolute()); break;
            case 0xde: c = 7; DEC(AddrAbsolute_X()); break;
            // DEX
            case 0xca: c = 2; DEX(); break;
            // DEY
            case 0x88: c = 2; DEY(); break;
            // EOR
            case 0x49: c = 2; EOR(GetImm()); break;
            case 0x45: c = 3; EOR(GetZP()); break;
            case 0x55: c = 4; EOR(GetZP_X()); break;
            case 0x4d: c = 4; EOR(GetAbsolute()); break;
            case 0x5d: c = 4; EOR(GetAbsolute_X()); break;
            case 0x59: c = 4; EOR(GetAbsolute_Y()); break;
            case 0x41: c = 6; EOR(GetIndirect_X()); break;
            case 0x51: c = 5; EOR(GetIndirect_Y()); break;
            // INC
            case 0xe6: c = 5; INC(AddrZP()); break;
            case 0xf6: c = 6; INC(AddrZP_X()); break;
            case 0xee: c = 6; INC(AddrAbsolute()); break;
            case 0xfe: c = 7; INC(AddrAbsolute_X()); break;
            // INX
            case 0xe8: c = 2; INX(); break;
            // INY
            case 0xc8: c = 2; INY(); break;
            // JMP
            case 0x4c: c = 3; JMP(pc); break;
            case 0x6c: c = 5; JMP(AddrAbsolute()); break;
            // JSR
            case 0x20: c = 6; JSR(); break;
            // LDA
            case 0xa9: c = 2; LDA(GetImm()); break;
            case 0xa5: c = 3; LDA(GetZP()); break;
            case 0xb5: c = 4; LDA(GetZP_X()); break;
            case 0xad: c = 4; LDA(GetAbsolute()); break;
            case 0xbd: c = 4; LDA(GetAbsolute_X()); break;
            case 0xb9: c = 4; LDA(GetAbsolute_Y()); break;
            case 0xa1: c = 6; LDA(GetIndirect_X()); break;
            case 0xb1: c = 5; LDA(GetIndirect_Y()); break;
            // LDX
            case 0xa2: c = 2; LDX(GetImm()); break;
            case 0xa6: c = 3; LDX(GetZP()); break;
            case 0xb6: c = 4; LDX(GetZP_Y()); break;
            case 0xae: c = 4; LDX(GetAbsolute()); break;
            case 0xbe: c = 4; LDX(GetAbsolute_Y()); break;
            // LDY
            case 0xa0: c = 2; LDY(GetImm()); break;
            case 0xa4: c = 3; LDY(GetZP()); break;
            case 0xb4: c = 4; LDY(GetZP_X()); break;
            case 0xac: c = 4; LDY(GetAbsolute()); break;
            case 0xbc: c = 4; LDY(GetAbsolute_X()); break;
            // LSR
            case 0x4a: c = 2; LSR(); break;
            case 0x46: c = 5; LSR(AddrZP()); break;
            case 0x56: c = 6; LSR(AddrZP_X()); break;
            case 0x4e: c = 6; LSR(AddrAbsolute()); break;
            case 0x5e: c = 7; LSR(AddrAbsolute_X()); break;
            // NOP
            case 0xea: c = 2; break;
            // ORA
            case 0x09: c = 2; ORA(GetImm()); break;
            case 0x05: c = 3; ORA(GetZP()); break;
            case 0x15: c = 4; ORA(GetZP_X()); break;
            case 0x0d: c = 4; ORA(GetAbsolute()); break;
            case 0x1d: c = 4; ORA(GetAbsolute_X()); break;
            case 0x19: c = 4; ORA(GetAbsolute_Y()); break;
            case 0x01: c = 6; ORA(GetIndirect_X()); break;
            case 0x11: c = 5; ORA(GetIndirect_Y()); break;
            // PHA
            case 0x48: c = 3; PHA(); break;
            // PHP
            case 0x08: c = 3; PHP(); break;
            // PLA
            case 0x68: c = 4; PLA(); break;
            // PLP
            case 0x28: c = 4; PLP(); break;
            // ROL
            case 0x2a: c = 2; ROL(); break;
            case 0x26: c = 5; ROL(AddrZP()); break;
            case 0x36: c = 6; ROL(AddrZP_X()); break;
            case 0x2e: c = 6; ROL(AddrAbsolute()); break;
            case 0x3e: c = 7; ROL(AddrAbsolute_X()); break;
            // ROR
            case 0x6a: c = 2; ROR(); break;
            case 0x66: c = 5; ROR(AddrZP()); break;
            case 0x76: c = 6; ROR(AddrZP_X()); break;
            case 0x6e: c = 6; ROR(AddrAbsolute()); break;
            case 0x7e: c = 7; ROR(AddrAbsolute_X()); break;
            // RTI
            case 0x40: c = 6; RTI(); break;
            // RTS
            case 0x60: c = 6; RTS(); break;
            // SBC
            case 0xe9: c = 2; SBC(GetImm()); break;
            case 0xe5: c = 3; SBC(GetZP()); break;
            case 0xf5: c = 4; SBC(GetZP_X()); break;
            case 0xed: c = 4; SBC(GetAbsolute()); break;
            case 0xfd: c = 4; SBC(GetAbsolute_X()); break;
            case 0xf9: c = 4; SBC(GetAbsolute_Y()); break;
            case 0xe1: c = 6; SBC(GetIndirect_X()); break;
            case 0xf1: c = 5; SBC(GetIndirect_Y()); break;
            // SEC
            case 0x38: c = 2; carry = true; break;
            // SED
            case 0xf8: c = 2; bcd = true; break;
            // SEI
            case 0x78: c = 2; idis = true; break;
            // STA
            case 0x85: c = 3; STA(AddrZP()); break;
            case 0x95: c = 4; STA(AddrZP_X()); break;
            case 0x8d: c = 4; STA(AddrAbsolute()); break;
            case 0x9d: c = 5; STA(AddrAbsolute_X()); break;
            case 0x99: c = 5; STA(AddrAbsolute_Y()); break;
            case 0x81: c = 6; STA(AddrIndirect_X()); break;
            case 0x91: c = 6; STA(AddrIndirect_Y()); break;
            // STX
            case 0x86: c = 3; STX(AddrZP()); break;
            case 0x96: c = 4; STX(AddrZP_Y()); break;
            case 0x8e: c = 4; STX(AddrAbsolute()); break;
            // STY
            case 0x84: c = 3; STY(AddrZP()); break;
            case 0x94: c = 4; STY(AddrZP_X()); break;
            case 0x8c: c = 4; STY(AddrAbsolute()); break;
            // TAX
            case 0xaa: c = 2; TAX(); break;
            // TAY
            case 0xa8: c = 2; TAY(); break;
            // TSX
            case 0xba: c = 2; TSX(); break;
            // TXA
            case 0x8a: c = 2; TXA(); break;
            // TXS
            case 0x9a: c = 2; TXS(); break;
            // TYA
            case 0x98: c = 2; TYA(); break;
                // UNHANDLED
            default:
               System.Console.WriteLine("Owey");
               break;
         }
         nmiCycleStart -= c;
         irqCycleStart -= c;

         if (pc < time.Length)
         {
            time[pc] += c;
         }
         return c;
      }

      bool checkInterrupt = false;
      bool NMILow = false;
      bool NMILastLow = false;
      bool IRQLow = false;

      long nmiCycleStart = 0;
      long irqCycleStart = 0;
      
      public void setIRQLow(bool low)
      {
         if (!IRQLow && low)
         {
            // If low -> will trigger an IRQ!
            irqCycleStart = 2;
         }
         checkInterrupt = true;
         IRQLow = low;
      }

      public void setNMILow(bool low)
      {
         if (!NMILow && low)
         {
            // If going from "high" to low -> will trigger an NMI!
            checkInterrupt = true;
            nmiCycleStart = 2;
         }
         NMILow = low;
         // If setting to non-low - both low and lastLow can be set?
         if (!low) NMILastLow = low;
      }

      bool CheckInterrupts()
      {
         bool interrupted = false;
         if (checkInterrupt)
         {
            // Trigger on negative edge!
            if (NMILow && !NMILastLow && nmiCycleStart < 0)
            {
               int status = PackSR() & 0xef;
               Interrupt(0xfffa, status);
               NMILastLow = NMILow;
               interrupted = true;
            }
            else if ((IRQLow && irqCycleStart < 0) || brk)
            {
               if (!idis)
               {
                  int status = PackSR() & 0xef;
                  if (brk)
                  {
                     status |= 0x10;
                     pc++;
                  }
                  Interrupt(0xfffe, status);
                  brk = false;
                  interrupted = true;
               }
               else
               {
                  brk = false;
                  checkInterrupt = (NMILow && !NMILastLow);
               }
            }
         }
         return interrupted;
      }

      void Interrupt(int addr, int status)
      {
         mem.write(sp--, pc >> 8);
         mem.write(sp--, pc & 255);
         mem.write(sp--, status);
         pc = mem.Peek16(addr);
         idis = true;
      }
   }
}
