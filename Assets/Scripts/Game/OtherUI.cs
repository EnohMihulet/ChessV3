using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;
using System;
using UnityEditor.Experimental.GraphView;
using Unity.VisualScripting;
using Unity.Mathematics;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.UI;

namespace Chess.Game 
{
    public class OtherUI : MonoBehaviour
    {
        public GameManager GameManager;
		public BoardUI BoardUI;

        // Buttons
        public GameObject StartButton;
		public GameObject PlayerColorButton;

        // Parents to display of captured pieces
		public GameObject WCapturedParent;
		public GameObject BCapturedParent;

        // List of piece prefabs of the captured pieces
		private List<GameObject> WCapturedPieces;
		private List<GameObject> BCapturedPieces;

        // Total white/black captured material
		public GameObject WMaterialCount; // White captured pieces value
		public GameObject BMaterialCount; // Black captured pieces value

		// Toggles
		public Toggle PVPToggle; // Player vs player
		public Toggle PVBToggle; // Player vs bot
		public Toggle BVBToggle; // Bot vs bot

		// Promotion UI
		public GameObject PromotionPanel;
		public GameObject WhitePromotionPieces;
		public GameObject BlackPromotionPieces;

		// Colors
		public Color white = Color.white;
		public Color gray = Color.gray;

        private bool HumanPlaysWhite;
        List<int> CapturedPieces;

        public void Start()
        {
            WCapturedPieces = new List<GameObject>();
			BCapturedPieces = new List<GameObject>();

            HumanPlaysWhite = GameManager.HumanPlaysWhite;
        }

		public void StartButtonPressed()
		{	
			GameManager.NewGame(HumanPlaysWhite);

            ClearCapturedPieces();
            ClearMaterialCounts();
		}

        public void ChangeColorButtonPressed()
		{
			HumanPlaysWhite = (HumanPlaysWhite == true) ? false : true;
            GameManager.HumanPlaysWhite = HumanPlaysWhite;

			// Change the text of the button
			TMP_Text buttonText = PlayerColorButton.GetComponentInChildren<TMP_Text>();
			
			if (HumanPlaysWhite)
			{
				buttonText.text = "White";
				buttonText.color = UnityEngine.Color.white;
			}
			else
			{
				buttonText.text = "Black";
				buttonText.color = UnityEngine.Color.black;
			}
		}

        public void UpdateCapturedPieces(Board board)
		{
			int blackMaterialCount = 0; // Amount of black material captured
			int whiteMaterialCount = 0; // Amount of white material captured
			int previousPiece = 0;
			Vector3 previousWPos = new Vector3(0,0,0); // Position of the last placed white piece
			Vector3 previousBPos = new Vector3(0,0,0); // Position of the last placed black piece

            CapturedPieces = board.CapturedPieces;

            if (CapturedPieces == null)
                return;

			if (CapturedPieces.Count != 0)
				CapturedPieces.Sort(); // Sorted by color then piece type: wp,wk,wb,...,bp,bk,bb,...
			
			// Clear old display of captured pieces and material count
			ClearCapturedPieces();
			ClearMaterialCounts();

            // Place all the captured pieces in their respective display
			foreach (int piece in CapturedPieces)
			{
				int pieceType = Piece.PieceType(piece);
				int pieceColor = Piece.PieceColor(piece);

				if (Piece.IsWhite(piece))
				{
					blackMaterialCount += Evaluation.PieceValues[pieceType];
					PlaceCapturedPieces(piece, previousPiece, ref previousWPos);
				}
				else
				{
					whiteMaterialCount += Evaluation.PieceValues[pieceType];
					PlaceCapturedPieces(piece, previousPiece, ref previousBPos);
				}
				previousPiece = piece;
			}

            // Places the material count of whoever is ahead
			PlaceMaterialCount(whiteMaterialCount, blackMaterialCount, previousWPos, previousBPos);
		}

        private void PlaceCapturedPieces(int piece, int previousPiece, ref Vector3 previousPos)
		{
			GameObject piecePrefab = GameManager.boardUI.PiecePrefabs[piece];
            // Offsets piece from previous piece depending on type (same type slightly stacks)
			if (previousPiece != 0 && Piece.PieceColor(piece) == Piece.PieceColor(previousPiece))
			{
				if (piece == previousPiece)
					previousPos.x = previousPos.x + .25f;
				else
					previousPos.x = previousPos.x + .5f;
			}
            // Instatiate captured piece and scale it down 
			GameObject placedPiece = Instantiate(piecePrefab, previousPos, quaternion.identity);
			placedPiece.transform.localScale = new Vector3(.6f,.6f,1.0f);
			
			// Set the captured piece's parents and adds piece to list of piece prefabs
			if (Piece.PieceColor(piece) == 0)
			{
				placedPiece.transform.SetParent(WCapturedParent.transform, false);

				WCapturedPieces.Add(placedPiece);
			}
			else
			{
				placedPiece.transform.SetParent(BCapturedParent.transform, false);

				BCapturedPieces.Add(placedPiece);
			}
		}

		private void PlaceMaterialCount(int whiteMaterialCount, int blackMaterialCount, Vector3 PreviousWPos, Vector3 PreviousBPos)
		{
			int difference = whiteMaterialCount - blackMaterialCount;
			int abvDiff = math.abs(difference);
			
			// No one is ahead in material
			if (difference == 0)
				return;

			// Black has more captured material
			if (whiteMaterialCount > blackMaterialCount)
			{
				BMaterialCount.SetActive(true);
				WMaterialCount.SetActive(false);
				TMP_Text BText = BMaterialCount.GetComponent<TMP_Text>();
				BText.text = ("+" + abvDiff);

				Vector3 pos = BMaterialCount.transform.position;
				pos.x = BCapturedPieces.Last<GameObject>().transform.position.x + 1f;
				BMaterialCount.transform.position = pos;
			}
			// White has more captured material
			else
			{
				WMaterialCount.SetActive(true);
				BMaterialCount.SetActive(false);
				TMP_Text WText = WMaterialCount.GetComponent<TMP_Text>();
				WText.text = ("+" + abvDiff);

				Vector3 pos = WMaterialCount.transform.position;
				pos.x = WCapturedPieces.Last<GameObject>().transform.position.x + 1f;
				WMaterialCount.transform.position = pos;
			}
		}

		private void ClearCapturedPieces()
		{
			if (WCapturedPieces.Count != 0)
			{
				foreach (GameObject piece in WCapturedPieces)
				{
					Destroy(piece);
				}
			}
			if (BCapturedPieces.Count != 0)
			{
				foreach (GameObject piece in BCapturedPieces)
				{
					Destroy(piece);
				}
			}
		}
		private void ClearMaterialCounts()
		{
			BMaterialCount.SetActive(false);
			WMaterialCount.SetActive(false);
		}

		public void TogglePressed()
		{
			GameManager.GameType type = GameManager.GameType.PVP;
			PVPToggle.GetComponentInChildren<Image>().color = white;
			PVBToggle.GetComponentInChildren<Image>().color = white;
			BVBToggle.GetComponentInChildren<Image>().color = white;

			if (PVPToggle.isOn)
			{
				type = GameManager.GameType.PVP;
				PVPToggle.GetComponentInChildren<Image>().color = gray;
			}
			else if (PVBToggle.isOn)
			{
				type = GameManager.GameType.PVB;
				PVBToggle.GetComponentInChildren<Image>().color = gray;
			}
			else if (BVBToggle.isOn)
			{
				type = GameManager.GameType.BVB;
				BVBToggle.GetComponentInChildren<Image>().color = gray;
			}
			
			GameManager.gameType = type;
		}

		public void SetPromotionUIActive()
		{
			PromotionPanel.SetActive(true);
			if (GameManager.colorToMove == 0)
				WhitePromotionPieces.SetActive(true);
			else
				BlackPromotionPieces.SetActive(true);

			BoardUI.WhitesideBoardLabels.SetActive(false);
			BoardUI.BlacksideBoardLabels.SetActive(false);
		}


		public void HidePromotionUI()
		{
			PromotionPanel.SetActive(false);
			WhitePromotionPieces.SetActive(false);
			BlackPromotionPieces.SetActive(false);

			if (GameManager.HumanPlaysWhite)
				BoardUI.WhitesideBoardLabels.SetActive(true);
			else
				BoardUI.BlacksideBoardLabels.SetActive(true);
		}


		public void PromotionButtonPressed(GameObject pressedPiece)
		{
			if (pressedPiece.tag == "queen")
				GameManager.promotionFlag = Move.PromoteToQueenFlag;
			else if (pressedPiece.tag == "knight")
				GameManager.promotionFlag = Move.PromoteToKnightFlag;
			else if (pressedPiece.tag == "bishop")
				GameManager.promotionFlag = Move.PromoteToBishopFlag;
			else
				GameManager.promotionFlag = Move.PromoteToRookFlag;

			HidePromotionUI();
		}
    }
}