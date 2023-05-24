using System;
using NAudio.Wave;

namespace Sink_20
{
   public class SoundChannels
   {
      WaveOut mWaveOut = new WaveOut();

      static float[] kChannelClocks = { 4329, 8659, 17329, 34640 };

      abstract class SoundChannelProvider : WaveProvider32
      {
         protected int mSample;

         public float mFrequency;
         public float mAmplitude;

         public SoundChannelProvider()
            : base( sampleRate: 22000, channels: 1 )
         {
         }
      }

      class SquareWaveProvider : SoundChannelProvider
      {
         public override int Read( float[] buf, int offset, int sampleCount )
         {
            int sampleRate = WaveFormat.SampleRate;
            for ( int n = 0; n < sampleCount; ++n )
            {
               float x = mSample * mFrequency / sampleRate;
               x -= (int) x;
               if ( x > 0.5 )
               {
                  buf[n + offset] = 0.0f;
               }
               else
               {
                  buf[n + offset] = mAmplitude;
               }
               mSample = ( mSample + 1 ) % sampleRate;
            }
            return sampleCount;
         }
      }

      class WhiteNoiseProvider : SoundChannelProvider
      {
         public Random mRandom = new Random();
         public float mCurrent;
         public bool mNeedNewRandom;

         public override int Read( float[] buf, int offset, int sampleCount )
         {
            int sampleRate = WaveFormat.SampleRate;
            for ( int n = 0; n < sampleCount; ++n )
            {
               float x = mSample * mFrequency / sampleRate;
               x -= (int) x;
               if ( x > 0.5 )
               {
                  if ( mNeedNewRandom )
                  {
                     mNeedNewRandom = false;
                     mCurrent = (float) mRandom.NextDouble();
                  }
               }
               else
               {
                  mNeedNewRandom = true;
               }
               buf[n + offset] = mAmplitude * mCurrent;
               mSample = ( mSample + 1 ) % sampleRate;
            }
            return sampleCount;
         }
      }

      SoundChannelProvider[] mChannels =
      {
         new SquareWaveProvider(),
         new SquareWaveProvider(),
         new SquareWaveProvider(),
         new WhiteNoiseProvider()
      };

      public SoundChannels()
      {
         mWaveOut.Init( new MixingWaveProvider32( mChannels ) );
         mWaveOut.Play();
      }

      public void Update( int[] chipMem )
      {
         // need to fill in the audio buffer too...
         int masterVol = chipMem[14] & 0xf;
         float masterGain = masterVol / 15.0f;
         for ( int i = 0; i < 4; ++i )
         {
            int val = chipMem[10 + i];
            if ( val > 127 && masterGain > 0 )
            {
               mChannels[i].mFrequency = kChannelClocks[i] / ( 255 - val );
               mChannels[i].mAmplitude = masterGain;
            }
            else
            {
               mChannels[i].mAmplitude = 0.0f;
            }
         }
      }
   }
}
