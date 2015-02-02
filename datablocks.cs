//-----------------------------------------------------------------------------
// Datablocks

datablock ParticleData(localChatParticle : painMidParticle)
{
	dragCoefficient		= 0.0;
	windCoefficient		= 0.0;
	gravityCoefficient	= 0.0;
	inheritedVelFactor	= 0.0;
	constantAcceleration	= 0.0;
	lifetimeMS		= 800;
	lifetimeVarianceMS	= 0;
	spinSpeed		= 0.0;
	spinRandomMin		= -0.0;
	spinRandomMax		= 0.0;
	useInvAlpha		= true;
	animateTexture		= false;

	textureName		= "base/data/particles/star1";
	
	colors[0]	= "0.2 0.6 1 0.5";
	colors[1]	= "0.2 0.6 1 0.5";
	colors[2]	= "0.2 0.6 1 0.5";
	sizes[0]	= 0.4;
	sizes[1]	= 0.8;
	sizes[2]	= 0.0;
	times[0]	= 0.0;
	times[1]	= 0.8;
	times[2]	= 1.0;
};

datablock ParticleEmitterData(localChatEmitter : painMidEmitter)
{
	ejectionPeriodMS = 5;
	periodVarianceMS = 0;
	ejectionVelocity = 0;
	velocityVariance = 0;
	ejectionOffset	= 1;
	thetaMin			= 0;
	thetaMax			= 0;
	phiReferenceVel  = 0;
	phiVariance		= 0;
	overrideAdvance = false;

	particles = localChatParticle;

	uiName = "Local Chat";
};

datablock ExplosionData(localChatExplosion)
{
	lifeTimeMS = 100;

	particleEmitter = localChatEmitter;
	particleDensity = 10;
	particleRadius = 0;

	faceViewer	  = false;
	explosionScale = "1 1 1";

	shakeCamera = false;

	hasLight	 = false;
	lightStartRadius = 0;
	lightEndRadius = 0;
	lightStartColor = "0.8 0.4 0.2";
	lightEndColor = "0.8 0.4 0.2";
};

datablock ProjectileData(localChatProjectile : bsdProjectile)
{
	explosion = localChatExplosion;
};