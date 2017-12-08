using System;

namespace FFImageLoading.Transformations
{
    [Flags]
    public enum CornerTransformType
    {
        TopLeftCut = 0x1,
        TopRightCut = 0x2,
        BottomLeftCut = 0x4,
        BottomRightCut = 0x8,

        TopLeftRounded = 0x10,
        TopRightRounded = 0x20,
        BottomLeftRounded = 0x40,
        BottomRightRounded = 0x80,

        AllCut = TopLeftCut | TopRightCut | BottomLeftCut | BottomRightCut,
        LeftCut = TopLeftCut | BottomLeftCut,
        RightCut = TopRightCut | BottomRightCut,
        TopCut = TopLeftCut | TopRightCut,
        BottomCut = BottomLeftCut | BottomRightCut,

        AllRounded = TopLeftRounded | TopRightRounded | BottomLeftRounded | BottomRightRounded,
        LeftRounded = TopLeftRounded | BottomLeftRounded,
        RightRounded = TopRightRounded | BottomRightRounded,
        TopRounded = TopLeftRounded | TopRightRounded,
        BottomRounded = BottomLeftRounded | BottomRightRounded,
    }
}