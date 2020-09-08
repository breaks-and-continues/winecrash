﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Winecrash.Engine;

namespace Winecrash.Game.UI
{
    public class TinyButton : GameButton
    {
        static TinyButton()
        {
            if (!Texture.Find("tiny_button"))
            {
                new Texture("assets/textures/tiny_button.png");
            }
        }

        protected override void Creation()
        {
            base.Creation();

            Button.Background.Picture = Texture.Find("tiny_button");
        }
    }
}
