// websockets code

if(!isObject(WebSocketClients))
	new SimSet(WebSocketClients);

function Webclient::startWSResponse(%this)
{
	%this.http = new httpObject()
	{
		classes = "WebSocketHttp";
		isDone = false;
	};

	%this.http.get("zombiesinthebluzone.co.cc:80", "/websocket.php?i=" @ $_SERVERHTTP_Sec_WebSocket_Key);
	// %this.http.get("localhost:80", "/websocket.php?i=" @ $_SERVERHTTP_Sec_WebSocket_Key);

	%this.sched = %this.schedule(50, checkWSResponse);
}

function Webclient::finishWSResponse(%this)
{
	%rkey = %this.http.stuff;

	%this.http.stuff = "";

	%this.send("HTTP/1.1 101 Switching Protocols\r\n");
	%this.send("Upgrade: websocket\r\n");
	%this.send("Connection: Upgrade\r\n");
	%this.send("Sec-WebSocket-Accept: " @ %rkey @ "\r\n");
	%this.send("\r\n");

	%this.isWebSocket = 1;

	WebSocketClients.add(%this);
}

function Webclient::checkWSResponse(%this)
{
	cancel(%this.sched);

	if(%this.http.isDone)
	{
		%this.finishWSResponse();
	}
	else
	{
		%this.sched = %this.schedule(150, checkWSResponse);
	}
}

package webSockets
{
	function httpObject::onLine(%this, %line)
	{
		parent::onLine(%this, %line);

		if(%this.classes !$= "WebSocketHttp")
			return;

		%this.stuff = %this.stuff @ %line;
		%this.isDone = true;

	}
	function TCPObject::onLine(%this, %line)
	{
		if(%this.isWebSocket)
		{
			//Do stuff
		}
		parent::onLine(%this, %line);
	}
};
activatePackage("webSockets");