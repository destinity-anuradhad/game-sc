const menuButton=document.querySelector('.menu-toggle');
const nav=document.querySelector('.site-nav');
menuButton?.addEventListener('click',()=>{
  const opened=nav.classList.toggle('open');
  menuButton.setAttribute('aria-expanded',String(opened));
});
document.querySelector('.nav-dropdown>button')?.addEventListener('click',event=>{
  if(window.matchMedia('(max-width: 850px)').matches){
    event.currentTarget.parentElement.classList.toggle('open');
  }
});

const cod4ShortName=document.querySelector('#ShortName');
const cod4RosterEntries=[...document.querySelectorAll('.main-player-entry, .substitute-entry')];
const refreshCod4Guids=()=>{
  if(!cod4ShortName||cod4RosterEntries.length===0)return;
  const shortName=cod4ShortName.value.trim().toUpperCase();
  const validShortName=/^[A-Z0-9]{2,4}$/.test(shortName);
  cod4RosterEntries.forEach(entry=>{
    const epfNumber=entry.querySelector('[data-epf-number]')?.value.trim().toUpperCase()??'';
    const guid=entry.querySelector('[data-cod4-guid]');
    const position=entry.querySelector('[data-guid-position]')?.dataset.guidPosition;
    const optionalSubstitute=entry.classList.contains('substitute-entry');
    guid.value=validShortName&&/^[A-Z0-9-]+$/.test(epfNumber)?`${shortName}_${epfNumber}_${position}`:'';
    if(optionalSubstitute&&!entry.querySelector('[name="Substitute"]')?.value.trim())guid.value='';
  });
};
cod4ShortName?.addEventListener('input',refreshCod4Guids);
document.querySelectorAll('[data-epf-number]').forEach(input=>input.addEventListener('input',refreshCod4Guids));
refreshCod4Guids();

const countdown=document.querySelector('[data-countdown-target]');
if(countdown){
  const deadline=Date.parse(countdown.dataset.countdownTarget);
  const fields={
    days:countdown.querySelector('[data-countdown-days]'),
    hours:countdown.querySelector('[data-countdown-hours]'),
    minutes:countdown.querySelector('[data-countdown-minutes]'),
    seconds:countdown.querySelector('[data-countdown-seconds]')
  };
  let countdownTimer;
  const updateCountdown=()=>{
    const remaining=Math.max(0,deadline-Date.now());
    const totalSeconds=Math.floor(remaining/1000);
    fields.days.textContent=String(Math.floor(totalSeconds/86400)).padStart(2,'0');
    fields.hours.textContent=String(Math.floor(totalSeconds%86400/3600)).padStart(2,'0');
    fields.minutes.textContent=String(Math.floor(totalSeconds%3600/60)).padStart(2,'0');
    fields.seconds.textContent=String(totalSeconds%60).padStart(2,'0');
    if(remaining===0){
      const heading=document.querySelector('.countdown-heading span:last-child');
      if(heading)heading.textContent='REGISTRATION CLOSED';
      clearInterval(countdownTimer);
    }
  };
  updateCountdown();
  if(deadline>Date.now())countdownTimer=setInterval(updateCountdown,1000);
}
