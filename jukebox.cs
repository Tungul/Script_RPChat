// Events
registerOutputEvent(GameConnection, listJukeBoxSongs, "", 1);
registerOutputEvent(fxDTSBrick, JUKEBOX_runSong, "");
registerOutputEvent(fxDTSBrick, JUKEBOX_stopSong, "");
registerOutputEvent(fxDTSBrick, JUKEBOX_pauseSong, "");
function GameConnection::listJukeboxSongs(%this)
{
	messageclient(%this, '', "\c3----");
	for(%i = 0; %i < $JukeboxSongs; %i++)
	{
		%songfile = $JukeboxSong[%i];
		messageClient(%this, '', "\c4[JUKEBOX]\c3[" @ %i @ "]" SPC strReplace(fileName(%songFile), ".txt", ""));
	}
	messageclient(%this, '', "\c3----");
}
function fxDTSBrick::JUKEBOX_runSong(%this){ %this.playSong("play"); }
function fxDTSBrick::JUKEBOX_stopSong(%this){ %this.playSong("stop"); }
function fxDTSBrick::JUKEBOX_pauseSong(%this){ %this.playSong("pause"); }

// start
function serverCmdJukebox(%this, %s)
{
	if(%s $= "")
		%this.listJukeboxSongs();
	// echo("playing song" SPC $JukeboxSong[%s]);
	%t = nameToID("_jukeBox");
	if(vectorDist(%t.getPosition(), %this.player.getPosition()) < 4)
		%t.jukeBox($JukeboxSong[%s]);
	else
		messageClient(%this, '', "\c4[JUKEBOX]\c6 You're too far away!");
}

// prepare
function fxDTSBrick::JukeBox(%this, %song)
{
	new fileObject("JukeboxFO")
	{
		delay = 3000;
		range = 4;
		song = "base/songs/" @ %song @ ".txt";
		playing = 1;
	};
	
	if(!isFile(JukeboxFO.song))
	{
		return;
	}
	
	JukeboxFO.openForRead(JukeboxFO.song);
	echo("playing song" SPC %song);
	%this.playSong();
}

// play
function fxDTSBrick::playSong(%this, %status)
{
	cancel(%this.songLoop); // do this first. not sure why.
	// echo("hi 1");
	if(!isObject(%this)) //if the brick was destroyed, stop playing the song.
	{
		JukeboxFO.close();
		JukeboxFO.delete();
		return;
	}
	// echo("hi 2");
	if(JukeboxFO.isEOF()) //if the song is over, stop playing the song.
	{
		JukeboxFO.close();
		JukeboxFO.delete();
		return;
	}
	// echo("hi 3");
	switch$(%status) // determine if we should pause, play, or stop.
	{
		case "play":
			JukeboxFO.playing = 1; // if play, play.
		case "pause":
			JukeboxFO.playing = 0; // if pause, then pause.
		case "stop":
			JukeboxFO.playing = 0; //if stop, then stop.
			JukeboxFO.close();
			JukeboxFO.delete();
			return;
	}
	// echo("hi 4");
	if(JukeboxFO.playing) //check if we're playing
	{
		%lyric = JukeboxFO.readLine(); //get the lyric
		if(%lyric $= "")
			%lyric = JukeboxFO.readLine();
		%this.bottomprintRadius("\c4[JUKEBOX]\c6 \x7E" SPC %lyric SPC "\x7E", JukeboxFO.range, JukeboxFO.delay); // play the lyric
		%this.schedule(1000, bottomprintRadius, "\c4[JUKEBOX]\c6 \x7E" SPC %lyric SPC "\x7E", JukeboxFO.range, JukeboxFO.delay);
		%this.schedule(2000, bottomprintRadius, "\c4[JUKEBOX]\c6 \x7E" SPC %lyric SPC "\x7E", JukeboxFO.range, JukeboxFO.delay);
	}
	// echo("high five!");
	%this.songLoop = %this.schedule(JukeboxFO.delay, playSong);	// loop
}

// support
function fxDTSBrick::messageRadius(%this, %msg, %radius)
{
	if(!isObject(%this))
		return;
	if(%radius $= "")
		%radius = 5;
	InitContainerRadiusSearch(%this.getTransform(), %radius, $TypeMasks::PlayerObjectType);
	while((%targetObject = containerSearchNext()) != 0) 
	{
		if(%targetObject.client !$= "" && %targetObject != %this)
		{
			messageClient(%targetObject.client, '', %msg);
		}
	}
}

function fxDTSBrick::bottomprintRadius(%this, %msg, %radius, %time)
{
	if(!isObject(%this))
		return;
	if(%radius $= "")
		%radius = 5;
	if(!%time)
		%time = 3000;
	%time = %time / 1000;
	InitContainerRadiusSearch(%this.getTransform(), %radius, $TypeMasks::PlayerObjectType);
	while((%targetObject = containerSearchNext()) != 0) 
	{
		if(%targetObject.client !$= "" && %targetObject != %this)
		{
			%targetObject.client.bottomprint(%msg, %time);
		}
	}
}

function populateJukeboxSongList()
{
	smp();
	$JukeboxSongs = 0;
	%songpath = "base/songs/*.txt";
	for(%SongFile = findFirstFile(%Songpath); %SongFile !$= ""; %SongFile = findNextFile(%Songpath))
	{
		$JukeboxSong[$JukeboxSongs] = strReplace(fileName(%songfile), ".txt", "");
		$JukeboxSongs++;
	}
}
populateJukeboxSongList();