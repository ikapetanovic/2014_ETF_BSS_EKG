/*
 * Ulaz.cs
 * Nedim Srndic
 * Biomedicinski signali i sistemi
 * Elektrotehnicki fakultet Univerziteta u Sarajevu
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace TestniEKG
{
    public abstract class Ulaz
    {
        public abstract void Read(int channel);
        public abstract void Stop();
    }
}
