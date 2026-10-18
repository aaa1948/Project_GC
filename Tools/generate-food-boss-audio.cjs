// Deterministic, original synthesized foley. No downloaded/licensed sound dependencies.
const fs=require('fs'),path=require('path');
const out=path.resolve(__dirname,'../Assets/Resources/FoodBosses/Audio');fs.mkdirSync(out,{recursive:true});
const rate=44100,events=['Shot','Radial','Dash','Land','Skill1','Skill2','Recover','Break','Phase','Death','Skid'];
const lengths=[.18,.48,.9,.62,1,.75,.85,.34,1.15,1.1,.55];let state=31843;
function noise(){state=(Math.imul(state,1664525)+1013904223)|0;return (state>>>0)/2147483648-1;}
function wave(t,f){return Math.sin(t*f*Math.PI*2);}const report=[];
for(let boss=0;boss<5;boss++)for(let event=0;event<events.length;event++){
 const len=lengths[event],n=Math.ceil(len*rate),samples=new Float32Array(n);let low=0,last=0;
 const pitch=[240,145,690,420,175][boss],wet=[.85,.08,.48,.1,.7][boss];
 for(let i=0;i<n;i++){
  const t=i/rate,u=t/len,white=noise();low+=.12*(white-low);const high=white-low;
  const onset=Math.min(1,t/.008),release=Math.min(1,(len-t)/.03);let v=0;
  if(event===0){const env=Math.exp(-u*6)*onset;v=(wave(t,pitch*(1-.55*u))*.65+low*(1-wet)*1.8+high*(boss===1?.22:.06))*env;}
  else if(event===1){const env=Math.sin(Math.PI*u)**1.5;v=(low*2.2+wave(t,pitch*(1+.7*u))*.18)*env;v+=high*Math.exp(-t*26)*.15;}
  else if(event===2){const env=Math.sin(Math.PI*u)**.65;v=(low*2+high*.10)*env;const beat=Math.pow(Math.max(0,wave(t,[13,11,4,100,7][boss])),5);v+=wave(t,pitch*(1-.45*u))*.2*beat*env;if(boss===3)v+=wave(t,118+u*55)*.16*env;}
  else if(event===3){const env=Math.exp(-u*6);v=wave(t,95-45*u)*.65*env+wave(t,170-65*u)*.23*env+low*2.2*Math.exp(-u*9);v+=high*(boss===1||boss===3?.24:.07)*Math.exp(-u*18);v+=wave(t,pitch*(1-u*.7))*wet*.15*Math.exp(-u*4);}
  else if(event===4||event===5){const env=Math.sin(Math.PI*u);const pulse=.35+.65*Math.max(0,wave(t,[17,8,38,13,5][boss]));v=low*1.8*env*pulse+wave(t,pitch*(1-.7*u))*.22*env;if(boss===3)v+=wave(t,360+u*450)*wave(t,5)*.2*env;if(boss===1)v+=high*.32*Math.pow(pulse,5)*env;if(boss===4)v+=wave(t,210*(1-u*.8))*.3*env;}
  else if(event===6){const env=Math.sin(Math.PI*u)*Math.exp(-u*1.5);v=wave(t,pitch*.5*(1+u*.7))*.35*env+low*1.8*wet*env+high*(1-wet)*.12*env;}
  else if(event===7){v=(high*.45*(1-wet)+low*2*wet+wave(t,620*(1-u*.7))*.2)*Math.exp(-u*7)*onset;if(boss===3)v+=(wave(t,2400)+wave(t,3500)*.4)*.15*Math.exp(-u*12);}
  else if(event===8){const env=Math.sin(Math.PI*u);v=(wave(t,75+55*u)*.4+wave(t,pitch*(.5+u*.6))*.15+low*.7)*env;}
  else if(event===9){const env=Math.sin(Math.PI*u)*(1-u);v=(wave(t,pitch*(1-.8*u))*.38+low*1.2*wet+high*.09*(1-wet))*env;}
  else {v=(low*2.2+high*.24)*Math.sin(Math.PI*u)*(.7+.3*wave(t,24));}
  v*=onset*release;const dc=v-last*.995;last=v;samples[i]=v;
 }
 let peak=0,sum=0;for(const v of samples)peak=Math.max(peak,Math.abs(v));const gain=.64/Math.max(.64,peak);
 const wav=Buffer.alloc(44+n*2);wav.write('RIFF');wav.writeUInt32LE(36+n*2,4);wav.write('WAVE',8);wav.write('fmt ',12);wav.writeUInt32LE(16,16);wav.writeUInt16LE(1,20);wav.writeUInt16LE(1,22);wav.writeUInt32LE(rate,24);wav.writeUInt32LE(rate*2,28);wav.writeUInt16LE(2,32);wav.writeUInt16LE(16,34);wav.write('data',36);wav.writeUInt32LE(n*2,40);
 for(let i=0;i<n;i++){const v=Math.max(-1,Math.min(1,samples[i]*gain));sum+=v*v;wav.writeInt16LE(Math.round(v*32767),44+i*2);}
 const name=boss+'-'+events[event]+'.wav';fs.writeFileSync(path.join(out,name),wav);report.push({name,seconds:len,peak:peak*gain,rms:Math.sqrt(sum/n)});
}
fs.writeFileSync(path.resolve(__dirname,'../Documentation/FoodBossAudioMeasurements.json'),JSON.stringify(report,null,2));console.log('Generated '+report.length+' mono PCM foley clips, 44.1kHz; peak <= 0.64');
