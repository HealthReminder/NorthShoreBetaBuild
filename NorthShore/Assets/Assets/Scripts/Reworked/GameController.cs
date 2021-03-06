﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
	public static GameController instance;
	private void Awake() {
		instance = this;
	}
	#region Troop Distribution
	public void DistributeTroops(PlayerData[] allPlayers,int currentTurn, int isScrambled){
		//Do it for every player
		int gaining = 0;
		print("Here 2.");
		foreach (PlayerData currentPlayer in allPlayers) {
			if(currentPlayer != null){
				//Set current troods gain to 0
				gaining = 0;
				//For balancing purpouses
				if(currentPlayer == GameManager.instance.playerManager.playerData){
					if(currentPlayer.provinces.Count <= 3)
						gaining+=1;
				} else {
					if(currentPlayer.provinces.Count >= 22)
						gaining-=1;
				}

				//Get new troods according to territory count
				int byTerrytory;
				if(isScrambled == 1)
					byTerrytory = currentPlayer.provinces.Count/2;
				else 
					byTerrytory = currentPlayer.provinces.Count/3;
				byTerrytory = Mathf.Clamp(byTerrytory,1,8);
				//Add to the troods gain
				gaining += byTerrytory;
				
				//DEBUG ONLY
				//if(currentPlayer == GameManager.instance.playerManager.playerData)
					//gaining*=10;
				//Get a factor within the turn
				//Players will get more troops inland the earlier
				//And more troops on the border the later
				int turnFactor = (currentTurn/8);
				turnFactor = Mathf.Clamp(turnFactor,0,6);
				//Turn 0  = 0
				//Turn 8 = 1
				//Turn 16 = 2

				//Distribute the troops to the heighest priority targets
				List<ProvinceData> targets = new List<ProvinceData>();
				foreach(ProvinceData d in currentPlayer.provinces) {
					int enemyNeighbourCount = 0;
					foreach(ProvinceData a in d.neighbours)
						if(a.owner != d.owner)
							enemyNeighbourCount++;

					if(enemyNeighbourCount != 0)
						enemyNeighbourCount+=turnFactor;
					else
						enemyNeighbourCount+=2-turnFactor;
						
					for(int a = enemyNeighbourCount-1; a >= 0 ;a--) {
						if(d.troops < 6)
							targets.Add(d);
					}
				}
				for( int a = gaining-1; a >=0 ; a--) {
					int randomSelect = Random.Range(0,targets.Count);
					if(randomSelect >0){
						if(targets[randomSelect]){
							targets[randomSelect].troops++;
							targets[randomSelect].UpdateGUI();
						} else
						Debug.Log("Critical error.");
					} else {
						Debug.Log("Index error. Skidding it.");
					}
				}
				Debug.Log("Player "+currentPlayer.playerInfo.name+" received "+gaining+" units.");
			}
		}
	}
	#endregion
	#region  Battling
	public IEnumerator Battle(ProvinceData attacker, ProvinceData defender) {
		
		//Player shots if visible
		if(defender.isAdjacentToPlayer)
			AudioManager.instance.PlayTrack("Player_Shots");
		

		//Calculate dice rolls
		int attackerValue, defenderValue;
		attackerValue = defenderValue = 0;
		for(int a = 0; a < attacker.troops; a++){
			int value = Random.Range(1,7);
			attackerValue+= value;
		}
		for(int g = 0; g < defender.troops;g++){
			int value = Random.Range(1,7);
			defenderValue+= value;
		}
		
		//Check who wins
		if(defenderValue >= attackerValue){
			//If the defender wins, the attacker loses all of its troops
			if(defender.troops>1)
				defender.troops -=1;
			attacker.troops = 1;
			defender.wasJustAttacked = true;
			defender.UpdateGUI();
			attacker.UpdateGUI();
		} else {
			PlayerData playerData = GameManager.instance.playerManager.playerData;
			AIManager manager = AIManager.instance;
			//If the attacker wins, the defender loses its spot. 
			//The attacking troops moves to the defending spot with one less troop, leaving one behind.
			//Take the province away from the owner
			if(playerData.playerInfo == defender.owner){
				//Debug.Log("The player is the loser");
				List<ProvinceData> playerProvinces = playerData.provinces;
				for(int f = playerProvinces.Count-1; f>=0; f--) {
					if(playerProvinces[f] == defender){
						//Debug.Log("Removed province from the player.");
						playerProvinces.RemoveAt(f);
					}else	Debug.Log("ERROR - Couldn't find the losing province owned by the player");
				}
			} else {
				manager.RemoveProvince(defender);	
			}
			//Give the province to another owner
			if(playerData.playerInfo.name == attacker.owner.name){
			//	Debug.Log("The player is the winner");
				playerData.provinces.Add(defender);
			} else {
				manager.AddProvince(defender,attacker.owner.name);
			}
			
			defender.troops = attacker.troops-1;
			defender.troops= Mathf.Clamp(defender.troops,1,99);
			attacker.troops = Mathf.Clamp(attacker.troops,1,99);
			attacker.troops = 1;

			defender.ChangeOwnerTo(attacker.owner);
			yield return null;
			defender.UpdateGUI();
			attacker.UpdateGUI();
			foreach(ProvinceData h in defender.neighbours)
				h.UpdateGUI();
			//print("Attacker won.");
		}
		defender.turnsOfEstability = 0;
		PlayerView.instance.Invoke("ClearBattleGUI",0.2f);
		yield break;
		
	}
	#endregion
}
