using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Chess.Core
{
    /*
    The format is as follows (ffffttttttssssss)
    Bits 0-5: start square index
    Bits 6-11: target square index
    Bits 12-15: flag
    */
    public class Move
    {
        // 16 bit move value
        private ushort moveValue;

        // Flags
		public const int NoFlag = 0b0000;
		public const int EnPassantCaptureFlag = 0b0001;
		public const int CastleFlag = 0b0010;
		public const int PawnTwoUpFlag = 0b0011;

		public const int PromoteToQueenFlag = 0b0100;
		public const int PromoteToKnightFlag = 0b0101;
		public const int PromoteToRookFlag = 0b0110;
		public const int PromoteToBishopFlag = 0b0111;

        // Masks
		const ushort StartSquareMask = 0b0000000000111111;
		const ushort TargetSquareMask = 0b0000111111000000;
		const ushort FlagMask = 0b1111000000000000;

        public Move(int startSquare, int targetSquare, uint flag)
        {
            moveValue = (ushort)(
                ((flag & 0xF) << 12)
                |  ((uint) startSquare    & 0x3F)
                | (((uint) targetSquare  & 0x3F) << 6)
            );
        }

        public Move(ushort moveValue)
        {
            this.moveValue = moveValue;
        }

        public Move(Coordinate startCoor, Coordinate targetCoor, uint flag)
        {
            moveValue = (ushort)(
                ((flag & 0xF) << 12)
                |  ((uint) (startCoor.rank * 8 + startCoor.file)    & 0x3F)
                | (((uint) (targetCoor.rank * 8 + targetCoor.file)  & 0x3F) << 6)
            );
        }

        public static Move NullMove => new Move(0);

        public bool IsNull => moveValue == 0;
        public int StartSquare => moveValue & StartSquareMask;
        public int TargetSquare => (moveValue & TargetSquareMask) >> 6;
        public bool IsPromotion => (moveValue >> 12) >= PromoteToQueenFlag;

        public int PromotionPieceType 
        {
            get 
            {
                switch (moveValue >> 12)
                {
                    case PromoteToBishopFlag: return Piece.Bishop;
                    case PromoteToKnightFlag: return Piece.Knight;
                    case PromoteToRookFlag:   return Piece.Rook;
                    case PromoteToQueenFlag:  return Piece.Queen;
                    default:                  return Piece.None;
                }
            }
        }

        public bool IsCastling => ((moveValue >> 12) & 0xF) == CastleFlag;
        public bool IsEnPassantCapture => ((moveValue >> 12) & 0xF) == EnPassantCaptureFlag;
        public bool IsDoubleMove => ((moveValue >> 12) & 0xF) == PawnTwoUpFlag;
    }
}