﻿using System;
using System.IO;

namespace FFImageLoading.Helpers
{
    public interface IMD5Helper
    {
        string MD5(string input);

        string MD5(Stream stream);
    }
}
