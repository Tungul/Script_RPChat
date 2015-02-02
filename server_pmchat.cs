// Project Miami - Separated RP Chat Module Release.
// By Shock (BL_ID 636).

$PMChat::OOC = true; // Define global var - OOC is on by default.

function PMChat_hasMoreCaps(%str) { // Function to check whether a string has more capital letters than non-capital letters.

	%caps = strlen(stripChars(%str, "abcdefghijklmnopqrstuvwxyz")); // Count capitals in string.
	%noncaps = strlen(stripChars(%str, "ABCDEFGHIJKLMNOPQRSTUVWXYZ")); // Count non-capitals in string.

	if (%caps > %noncaps) {
		return true;
	} else {
		return false;
	}

}

function PMChat_fixGrammar(%str) { // Function to capitalize the first letter of a word, and make all the other letters lower-case.

	%corrected = "";

	for (%i = 0; %i < strlen(%str); %i++) {
		%char = getSubStr(%str, %i, 1);
		if (%i == 0) {
			%char = strupr(%char);
		} else {
			%char = strlwr(%char);
		}
		%corrected = %corrected @ %char;
	}

	return stripMLControlChars(%corrected);

}

function PMChat_nameTaken(%client, %name) { // Function to determine if an RP name is already taken.

	for (%i = 0; %i < clientGroup.getCount(); %i++) {
		%c = clientGroup.getObject(%i);

		if ( (%c.rpname $= %name) && (%c != %client) ) {
			return true; // Name is taken.
		}
	}

	return false; // Name not taken.

}

function serverCmdWho(%client) { // Command to see a list of current online users with their RP names. Administrative purposes, usually.

	for (%i = 0; %i < clientGroup.getCount(); %i++) {
		%c = clientGroup.getObject(%i);
		messageClient(%client, '', "\c3" @ %c.rpname @ "\c6's username is \c3" @ %c.name @ "\c6.");
	}

}

function serverCmdRpname(%client, %n1, %n2) { // Set your RP name!

	if (%n1 $= "" || %n2 $= "") {
		messageClient(%client, '', "\c0Please enter a first name and surname.");
		return;
	}

	if (strlen(%n1) < 4 || strlen(%n2) < 4) {
		messageClient(%client, '', "\c0Your first name and surname must contain at least 4 characters.");
		return;
	}

	if (strlen(%n1) > 12 || strlen(%n2) > 12) {
		messageClient(%client, '', "\c0Your first name or surname contains more than 12 characters.");
		return;
	}

	if (stripChars(%n1, "[0123456789'{}[\]\\;':\",./?!@#$%&*()_+=<>^-]") !$= %n1 || stripChars(%n2, "[0123456789'{}[\]\\;':\",./?!@#$%&*()_+=<>^-]") !$= %n2) {
		messageClient(%client, '', "\c0Your first name or surname contains illegal characters.");
		return;
	}

	%compiled = trim(PMChat_fixGrammar(%n1) SPC PMChat_fixGrammar(%n2)); // Fix that grammar and compile both the first/surname.
	
	if (%client.rpname $= %compiled) {
		messageClient(%client, '', "\c0Your RP name is already set to that!");
		return;
	}
	
	if (PMChat_nameTaken(%client, %compiled) == true) {
		messageClient(%client, '', "\c0That name has been taken.");
		return;
	}

	messageAll('', "\c3" @ %client.name @ "\c6 has changed his RP name to \c3" @ %compiled @ "\c6.");

	echo(%client.name @ " has changed his RP name to " @ %compiled @ "."); // Simple logging to console.log

	%client.rpname = %compiled; // Set the RP name for the client.

	$PMChat::SavedNames[%client.bl_id] = %compiled; // Save it for the duration of the server's operation.
}

function serverCmdToggleOOC(%client) { // Enable or disable OOC.

	if (!%client.isAdmin) { // Admin and Superadmin only.
		messageClient(%client, '', "\c0You do not have access to this command.");
		return;
	}

	if ($PMChat::OOC == true) {
		$PMChat::OOC = false;
		messageAll('', "\c3" @ %client.name @ " \c0disabled OOC.");
		echo(%client.name @ " disabled OOC."); // Simple logging to console.log
	} else {
		$PMChat::OOC = true;
		messageAll('', "\c3" @ %client.name @ " \c0enabled OOC.");
		echo(%client.name @ " enabled OOC."); // Simple logging to console.log
	}

}

function serverCmdOOC(%client, %m1, %m2, %m3, %m4, %m5, %m6, %m7, %m8, %m9, %m10, %m11, %m12, %m13, %m14, %m15, %m16, %m17, %m18, %m19, %m20, %m20, %m22, %m23, %m24) {

	if (%m1 $= "") { // Stop if there are no words at all.
		return;
	}

	if (($PMChat::OOC $= false) && (!%client.isAdmin)) { // If OOC is disabled and the player isn't Admin, don't allow them to use OOC.
		messageClient(%client, '', "\c0OOC has been disabled.");
		return;
	}

	%compiled = %m1 SPC %m2 SPC %m3 SPC %m4 SPC %m5 SPC %m6 SPC %m7 SPC %m8 SPC %m9 SPC %m10 SPC %m11 SPC %m12 SPC %m13 SPC %m14 SPC %m15 SPC %m16 SPC %m17 SPC %m18 SPC %m19 SPC %m20 SPC %m20 SPC %m22 SPC %m23 SPC %m24;

	%compiled = stripMLControlChars(%compiled); // Strip Torque Markup Language from the message.

	messageAll('', "\c3(OOC) " @ %client.name @ "\c6: " @ trim(%compiled) );

	echo("(OOC) " @ %client.name @ ": " @ trim(%compiled) );

}

function serverCmdLOOC(%client, %m1, %m2, %m3, %m4, %m5, %m6, %m7, %m8, %m9, %m10, %m11, %m12, %m13, %m14, %m15, %m16, %m17, %m18, %m19, %m20, %m20, %m22, %m23, %m24) {

	if (%m1 $= "") { // Stop if there are no words at all.
		return;
	}

	if (!isObject(%client.player)) { // Stop if the client doesn't have a player object.
		return;
	}

	if (($PMChat::OOC $= false) && (!%client.isAdmin)) {
		messageClient(%client, '', "\c0OOC has been disabled.");
		return;
	}

	%compiled = %m1 SPC %m2 SPC %m3 SPC %m4 SPC %m5 SPC %m6 SPC %m7 SPC %m8 SPC %m9 SPC %m10 SPC %m11 SPC %m12 SPC %m13 SPC %m14 SPC %m15 SPC %m16 SPC %m17 SPC %m18 SPC %m19 SPC %m20 SPC %m20 SPC %m22 SPC %m23 SPC %m24;

	%compiled = stripMLControlChars(%compiled); // Strip Torque Markup Language from the message.

	InitContainerRadiusSearch(%client.player.position, 16, $TypeMasks::PlayerObjectType); // Check roughly 16 studs around the client for other clients.
	while ((%targetobject=containerSearchNext()) !$= 0) {
		%clients = %targetobject.client;
		messageClient(%clients, '', "\c3(LOOC) " @ %client.name @ "\c6: " @ trim(%compiled) ); // Tell everyone within roughly 16 studs the LOOC message.
	}

	echo("(LOOC) " @ %client.name @ ": " @ trim(%compiled) ); // Simple logging to console.log

}

function serverCmdMe(%client, %m1, %m2, %m3, %m4, %m5, %m6, %m7, %m8, %m9, %m10, %m11, %m12, %m13, %m14, %m15, %m16, %m17, %m18, %m19, %m20, %m20, %m22, %m23, %m24) {

	if (%m1 $= "") { // Stop if there are no words at all.
		return;
	}

	if (!isObject(%client.player)) {
		messageClient(%client, '', "\c0You're not alive!"); // Stop if the client doesn't have a player object.
		return;
	}

	%compiled = %m1 SPC %m2 SPC %m3 SPC %m4 SPC %m5 SPC %m6 SPC %m7 SPC %m8 SPC %m9 SPC %m10 SPC %m11 SPC %m12 SPC %m13 SPC %m14 SPC %m15 SPC %m16 SPC %m17 SPC %m18 SPC %m19 SPC %m20 SPC %m20 SPC %m22 SPC %m23 SPC %m24;

	%compiled = stripMLControlChars(%compiled); // Strip Torque Markup Language from the message.

	InitContainerRadiusSearch(%client.player.position, 16, $TypeMasks::PlayerObjectType); // Check roughly 16 studs around the client for other clients.
	while ((%targetobject=containerSearchNext()) !$= 0) {
		%clients = %targetobject.client;
		messageClient(%clients, '', "\c3(Me) " @ %client.rpname @ "\c6 " @ trim(%compiled) ); // Tell everyone within roughly 16 studs the Me message.
	}

	echo("(Me) " @ %client.rpname @ " " @ trim(%compiled) ); // Simple logging to console.log

}

function serverCmdPm(%client, %client2, %m1, %m2, %m3, %m4, %m5, %m6, %m7, %m8, %m9, %m10, %m11, %m12, %m13, %m14, %m15, %m16, %m17, %m18, %m19, %m20, %m20, %m22, %m23, %m24) {

	if (%client2 $= "") { // If no person is specified to send the message to - stop.
		return;
	}

	%client2 = findClientByName(%client2); // Find that person who needs to receive the message

	if (%client2 == false) {
		messageClient(%client, '', "\c0Invalid user specified."); // Typo in the name or the person actually doesn't exist on the server - stop here.
		return;
	}

	if (%client2 == %client) {
		messageClient(%client, '', "\c0You're trying to speak to yourself."); // are you dumb.
		return;
	}

	%compiled = %m1 SPC %m2 SPC %m3 SPC %m4 SPC %m5 SPC %m6 SPC %m7 SPC %m8 SPC %m9 SPC %m10 SPC %m11 SPC %m12 SPC %m13 SPC %m14 SPC %m15 SPC %m16 SPC %m17 SPC %m18 SPC %m19 SPC %m20 SPC %m20 SPC %m22 SPC %m23 SPC %m24;

	%compiled = stripMLControlChars(%compiled); // Strip Torque Markup Language from the message.

	messageClient(%client2, '', "\c3(PM) " @ %client.rpname @ "\c6: " @ trim("\c4" @ %compiled) ); // Show message to client who receives it.
	messageClient(%client, '', "\c3(PM) " @ %client.rpname @ "\c6: " @ trim("\c4" @ %compiled) ); // Show message to client who sent it.

	echo("(PM) " @ %client.rpname @ " -> " @ %client2.rpname @ ": " @ trim(%compiled) ); // Simple logging to console.log

}

package PMChat {

	function serverCmdMessageSent(%client, %msg) { // Normal, local chat.

		if (%msg $= "") { // Stop if there are no words at all.
			return;
		}

		if (!isObject(%client.player)) { // Stop if the client doesn't have a player object.
			messageClient(%client, '', "\c0You're not alive! You may use /OOC instead.");
			return;
		}

		%msg = stripMLControlChars(%msg); // Strip Torque Markup Language from the message.

		%client.player.playThread(3, talk); // Play some talk animation.
		%client.player.schedule(500, playThread, 3, root);

		if (getWord(%msg, 0) $= "*y") { // If the first word is "*y", the player wants to yell.

			if (getWord(%msg, 1) $= "") { // If yelling and no words are specified - stop.
				return;
			}

			InitContainerRadiusSearch(%client.player.position, 48, $TypeMasks::PlayerObjectType); // Check roughly 48 studs around the player for others.
			while ((%targetobject=containerSearchNext()) !$= 0) {
				%clients = %targetobject.client;
				messageClient(%clients, '', "\c3" @ %client.rpname @ "\c6 yells, \"" @ trim(strReplace(%msg, getWord(%msg, 0), "")) @ "\""); // Message any players roughly 48 studs around the player who yelled.
			}

			echo(%client.rpname @ " yells, \"" @ trim(strReplace(%msg, getWord(%msg, 0), "")) @ "\""); // Simple logging to console.log

		} else if (getWord(%msg, 0) $= "*w") { // If the first word is "*w", the player wants to whisper.

			if (getWord(%msg, 1) $= "") { // If whispering and no words are specified - stop.
				return;
			}

			InitContainerRadiusSearch(%client.player.position, 4, $TypeMasks::PlayerObjectType); // Check roughly 4 studs around the player for others.
			while ((%targetobject=containerSearchNext()) !$= 0) {
				%clients = %targetobject.client;
				messageClient(%clients, '', "\c3" @ %client.rpname @ "\c6 whispers, \"" @ trim(strReplace(%msg, getWord(%msg, 0), "")) @ "\""); // Message any players roughly 4 studs around the player who whispered.
			}

			echo(%client.rpname @ " whispers, \"" @ trim(strReplace(%msg, getWord(%msg, 0), "")) @ "\""); // Simple logging to console.log

		} else {

			if ((strPos(%msg, "!") > -1) || (PMChat_hasMoreCaps(%msg))){ // If the player has more caps than non-caps in his message, or uses an exclamation mark - he's exclaiming.
				%type = "exclaims";
			} else if (strPos(%msg, "?") > -1) { // If the message has a question mark, the player is asking a question.
				%type = "asks";
			} else { // If none of the above applies, he's just talking normally.
				%type = "says";
			}

			InitContainerRadiusSearch(%client.player.position, 32, $TypeMasks::PlayerObjectType); // Check roughly 32 studs around the player for others.
			while ((%targetobject=containerSearchNext()) !$= 0) {
				%clients = %targetobject.client;
				messageClient(%clients, '', "\c3" @ %client.rpname @ "\c6 " @ %type @ ", \"" @ trim(%msg) @ "\""); // Message any players roughly 32 studs around the player who talked.
			}

			echo(%client.rpname @ " " @ %type @ ", \"" @ trim(%msg) @ "\""); // Simple logging to console.log

		}

	}

	function serverCmdTeamMessageSent(%client, %msg) { // Radio chat!

		if (%msg $= "") { // Stop if there are no words at all.
			return;
		}

		if (!isObject(%client.player)) { // Stop if the client doesn't have a player object.
			messageClient(%client, '', "\c0You're not alive!");
			return;
		}

		%msg = stripMLControlChars(%msg); // Strip Torque Markup Language from the message.

		%client.player.playThread(3, talk); // Play some talk animation.
		%client.player.schedule(500, playThread, 3, root);

		if (getWord(%msg, 0) $= "*y") { // If the first word is "*y", the player wants to yell via radio.

			if (getWord(%msg, 1) $= "") { // If yelling and no words are specified - stop.
				return;
			}

			messageAll('', "\c6[\c7RADIO\c6] \c3" @ %client.rpname @ "\c6 yells, \"" @ trim(strReplace(%msg, getWord(%msg, 0), "")) @ "\""); // Message to all players via radio.

			echo("[RADIO] " @ %client.rpname @ "\c6 yells, \"" @ trim(strReplace(%msg, getWord(%msg, 0), "")) @ "\""); // Simple logging to console.log

		} else if (getWord(%msg, 0) $= "*w") { // If the first word is "*w", the player wants to whisper via radio (pointless, lol?)

			if (getWord(%msg, 1) $= "") { // If whispering and no words are specified - stop.
				return;
			}

			messageAll('', "\c6[\c7RADIO\c6] \c3" @ %client.rpname @ "\c6 whispers, \"" @ trim(strReplace(%msg, getWord(%msg, 0), "")) @ "\""); // Message to all players via radio.

			echo("[RADIO] " @ %client.rpname @ "\c6 whispers, \"" @ trim(strReplace(%msg, getWord(%msg, 0), "")) @ "\""); // Simple logging to console.log

		} else {

			if ((strPos(%msg, "!") > -1) || (PMChat_hasMoreCaps(%msg))){ // If the player has more caps than non-caps in his message, or uses an exclamation mark - he's exclaiming.
				%type = "exclaims";
			} else if (strPos(%msg, "?") > -1) { // If the message has a question mark, the player is asking a question.
				%type = "asks";
			} else { // If none of the above applies, he's just talking normally.
				%type = "says";
			}

			messageAll('', "\c6[\c7RADIO\c6] \c3" @ %client.rpname @ "\c6 " @ %type @ ", \"" @ trim(%msg) @ "\""); // Message to all players via radio.

			echo("[RADIO] " @ %client.rpname @ " " @ %type @ ", \"" @ trim(%msg) @ "\""); // Simple logging to console.log

		}

	}

	function GameConnection::onClientEnterGame(%client) {

		%savedname = $PMChat::SavedNames[%client.bl_id];

		if (%savedname !$= "") { // Check to see if the client already has an RP name set whilst the server was and still is online.
			if (PMChat_nameTaken(%client, %savedname) == true) {
				messageClient(%client, '', "\c6Whoops! Someone else took your RP name while you were gone. Select a new one with /rpname [first] [last].");
				%client.rpname = %client.name;
			} else {
				messageClient(%client, '', "\c6I remember your RP name! Your RP name has been reloaded to \c3" @ %savedname @ "\c6.");
				%client.rpname = %savedname; // Reload the client's RP name.
			}
		} else { // If no previous saved name is found, or the server crashed/restarted/exploded...
			messageClient(%client, '', "\c6You don't have an RP name! You can set one yourself using /rpname [first] [last].");
			%client.rpname = %client.name; // Set client's RP name to client's name until changed with /rpname
		}

		parent::onClientEnterGame(%client);

	}

};

activatePackage(PMChat);