using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace Sink_20
{
   public partial class Emulator : DockContent
   {
      Sink20 mSink;
      Stopwatch mStopwatch = new Stopwatch();
      double mLastTime;

      public bool mHasFocus;

      void LoadROMs( Memory mem )
      {
         mem.Fill( 0x8000, File.ReadAllBytes( "chargen" ), 0 );
         mem.Fill( 0xc000, File.ReadAllBytes( "basic" ), 0 );
         mem.Fill( 0xe000, File.ReadAllBytes( "kernal" ), 0 );
      }

      bool ReadyForProgram( CPU cpu )
      {
         // is the cpu in the loop waiting for a key?
         return cpu.pc == 0xe5e8;
      }

      public void LoadProgram( byte[] prg )
      {
         Memory mem = mMemory;
         int loadAddr = prg[0] + ( prg[1] << 8 );
         mem.Fill( loadAddr, prg, 2 );
         // set Start of Basic
         mem.Poke16( 43, loadAddr );
         int startOfVariables = loadAddr + prg.Length - 2;
         mem.Poke16( 45, startOfVariables );
         mem.Poke16( 47, startOfVariables );
         mem.Poke16( 49, startOfVariables );
         Console.WriteLine( $"Program loaded at {loadAddr:x4}" );
         //SYS50483 to relocate
      }

      public void PokeCommandIntoKeyboardBuffer( string command )
      {
         Memory mem = mMemory;
         for ( int i = 0; i < command.Length; ++i )
         {
            mem.Poke( 631 + i, command[i] );
         }
         // poke ENTER
         mem.Poke( 631 + command.Length, 13 );
         mem.Poke( 198, command.Length + 1 );
      }

      enum EStartupState
      {
         Starting,
         Loaded,
         Relocating,
         Running
      }

      Keyboard mKeyboard = new Keyboard();
      public Memory mMemory = new Memory();
      public CPU mCPU = new CPU();
      VIA mVIA1 = new VIA();
      VIA mVIA2 = new VIA();
      VIC mVIC = new VIC();

      long mCycle = 0;

      public bool BreakOnLineChange { get; set; }
      public List<int> mBreakpoints = new List<int>();

      public bool IsPaused()
      {
         return !mStopwatch.IsRunning;
      }

      public int BASICLineNumber()
      {
         return mMemory.Peek16( 57 );
      }

      public int BASICExecutePtr()
      {
         return mMemory.Peek16( 0x7a );
      }

      int mOldLine;
      int mCyclesForBASICLine;
      int mAccumulatedCycles;

      Dictionary<int, long> mProfileData = new Dictionary<int, long>();

      bool mIsProfiling;
      public bool IsProfiling
      {
         get
         {
            return mIsProfiling;
         }
         set
         {
            mIsProfiling = value;
            if ( mIsProfiling )
            {
               mOldLine = -1;
               mCyclesForBASICLine = 0;
               mAccumulatedCycles = 0;
            }
         }
      }

      public void ClearProfileData()
      {
         mProfileData = new Dictionary<int, long>();
      }

      public void AddProfileData( bool lineChange )
      {
         if ( IsProfiling && mOldLine >= 0 )
         {
            if ( lineChange || mCyclesForBASICLine > VIC.CLOCK_FREQUENCY )
            {
               long x = 0;
               mProfileData.TryGetValue( mOldLine, out x );
               mProfileData[mOldLine] = x + mCyclesForBASICLine;
               mCyclesForBASICLine = 0;

            }
            if ( mAccumulatedCycles > VIC.CLOCK_FREQUENCY / 3 )
            {
               mAccumulatedCycles = 0;
               mSink.editor.PaintEditor();
            }
         }
      }

      public Dictionary<int, long> GetProfileData()
      {
         return mProfileData;
      }

      public void Think()
      {
         if ( mStopwatch.IsRunning )
         {
            double time = mStopwatch.Elapsed.TotalSeconds;
            double elapsed = time - mLastTime;
            mLastTime = time;

            // limit clock
            long wantedElapsedCycles = (long) ( elapsed * VIC.CLOCK_FREQUENCY );
            long elapsedCycles = Math.Min( wantedElapsedCycles, 25000 );

            long newCycle = mCycle + elapsedCycles;

            while ( mCycle < newCycle )
            {
               int instructionCycles = mCPU.Update();
               mCycle += instructionCycles;
               mVIA1.Update( mCycle );
               mVIA2.Update( mCycle );
               mVIC.Update( mCycle );

               if ( IsProfiling && mOldLine >= 0 )
               {
                  mCyclesForBASICLine += instructionCycles;
                  mAccumulatedCycles += instructionCycles;
                  if ( mCyclesForBASICLine > VIC.CLOCK_FREQUENCY )
                  {
                     AddProfileData( lineChange: false );
                  }
               }

               if ( mCPU.pc == 0xc7e1 ) // do start new basic code
               {
                  int newLine = BASICLineNumber();
                  if ( newLine != mOldLine )
                  {
                     AddProfileData( lineChange: true );
                     mOldLine = newLine;
                     if ( BreakOnLineChange
                        || mBreakpoints.Contains( newLine ) )
                     {
                        mStopwatch.Stop();
                        BreakOnLineChange = true;
                        mSink.editor.Break( newLine );
                        mSink.editor.PaintEditor();
                        mSink.callStack.PaintCallStack();
                        mSink.variables.PaintVariables();
                        mSink.editor.Activate();
                        break;
                     }
                  }
                  if ( mSink.variables.VariableChanged() )
                  {
                     mStopwatch.Stop();
                     BreakOnLineChange = true;
                     mSink.editor.Break( newLine );
                     mSink.editor.PaintEditor();
                     mSink.callStack.PaintCallStack();
                     mSink.variables.PaintVariables();
                     mSink.variables.Activate();
                     break;
                  }
               }
            }
         }
      }

      public Emulator( Sink20 _sink )
      {
         InitializeComponent();

         mSink = _sink;

         LoadROMs( mMemory );

         // interconnections
         mCPU.Init( mMemory );
         mVIA1.Init( mCPU, mKeyboard, 0x9110 );
         mVIA2.Init( mCPU, mKeyboard, 0x9120 );
         mVIC.Init( mCPU, mMemory, pictureBox );
         mMemory.AddChip( mVIA1 );
         mMemory.AddChip( mVIA2 );
         mMemory.AddChip( mVIC );

         mStopwatch.Start();
      }

      public bool IsRunning()
      {
         return mStopwatch.IsRunning;
      }

      public void DebugPlay()
      {
         mStopwatch.Start();
         BreakOnLineChange = false;
      }

      public void DebugStep()
      {
         mStopwatch.Start();
         BreakOnLineChange = true;
      }

      public void DebugPause()
      {
         BreakOnLineChange = true;
         // todo: also break mid line (in case there's an infinite loop)
      }

      public void ToggleBreakpoint( int line )
      {
         if ( mBreakpoints.Contains( line ) )
         {
            mBreakpoints.Remove( line );
         }
         else
         {
            mBreakpoints.Add( line );
         }
      }

      public bool HasBreakpoint( int line )
      {
         return mBreakpoints.Contains( line );
      }

      public bool HasBreakpoints()
      {
         return mBreakpoints.Count > 0;
      }

      public void GotKeyDown( KeyEventArgs e )
      {
         mKeyboard.KeyDown( e.KeyValue, true );
         e.Handled = true;
      }

      public void GotKeyUp( KeyEventArgs e )
      {
         mKeyboard.KeyDown( e.KeyValue, false );
         e.Handled = true;
      }

      private void Emulator_Enter( object sender, EventArgs e )
      {
         mHasFocus = true;
         Console.WriteLine( "Emulator_Enter" );
      }

      private void Emulator_Leave( object sender, EventArgs e )
      {
         mHasFocus = false;
         Console.WriteLine( "Emulator_Leave" );
      }
   }
}
