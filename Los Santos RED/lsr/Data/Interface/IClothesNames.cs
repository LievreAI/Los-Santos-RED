﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LosSantosRED.lsr.Interface
{
    public interface IClothesNames
    {
        string GetName(bool isProp, int ItemID, int DrawableID, int TextureID, string Gender);
    }
}
