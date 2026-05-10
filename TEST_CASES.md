# Test Cases — Zapomenuté podzemí (MUD)

## Testovací účty

| Typ hráče | Uživatelské jméno | Heslo | Stav před testem |
|---|---|---|---|
| Existující hráč | test_player | Test123 | účet existuje, startovní místnost |
| Hráč s uloženým stavem | saved_player | Save123 | má uloženy předměty v inventáři, je v místnosti `zbrojnice` |
| Neexistující hráč | ghost_player_999 | — | účet nesmí existovat |
| Hráč pro registraci | new_player_001 | New123 | účet nesmí existovat |
| Druhý hráč (multi-player) | player_two | Player2 | účet existuje |

> **Poznámka:** Účty `test_player` a `saved_player` musí být vytvořeny před spuštěním testů (poprvé se přihlásit a zadat heslo).

---

## Příprava prostředí

- **Server:** spuštěn příkazem `dotnet run` ve složce `MudServer/` na hostiteli (IP: `127.0.0.1`, port: `4000`)
- **Klient:** spuštěn příkazem `dotnet run -- 127.0.0.1 4000` ve složce `MudClient/`
- **Operační systém:** libovolný s .NET 8 SDK

---

## MVP funkce hry (min. 10 testů)

### TC-MVP-01 — Spuštění serveru

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-01 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – spuštění serveru |
| **Předpoklady** | .NET 8 SDK nainstalováno, soubor `config.json` existuje |

**Kroky:**
1. V terminálu přejdi do složky `MudServer/`.
2. Spusť příkaz `dotnet run`.

**Očekávaný výsledek:**  
Konzole zobrazí řádky:
```
[YYYY-MM-DD HH:MM:SS] [INFO] === Server se spousti ===
[YYYY-MM-DD HH:MM:SS] [INFO] Herni svet nacten.
[YYYY-MM-DD HH:MM:SS] [INFO] Server nasloucha na portu 4000.
```
Server zůstane spuštěný a čeká na připojení.

---

### TC-MVP-02 — Připojení klienta

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-02 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – připojení klienta |
| **Předpoklady** | Server běží (TC-MVP-01 prošel) |

**Kroky:**
1. V novém terminálu přejdi do složky `MudClient/`.
2. Spusť příkaz `dotnet run -- 127.0.0.1 4000`.

**Očekávaný výsledek:**  
Klient zobrazí:
```
✓ Připojeno!  Odejdi příkazem: Ctrl+C

╔══════════════════════════════╗
║  Vítej v Zapomenutém podzemí ║
╚══════════════════════════════╝

Zadej své jméno:
```

---

### TC-MVP-03 — Připojení více klientů současně

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-03 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – více klientů |
| **Předpoklady** | Server běží |

**Kroky:**
1. Spusť **dva** klienty (`dotnet run -- 127.0.0.1 4000`) v oddělených terminálech.
2. V prvním klientu se přihlas jako `test_player` / `Test123`.
3. Ve druhém klientu se přihlas jako `player_two` / `Player2`.

**Očekávaný výsledek:**  
Oba klienti jsou přihlášeni a každý vidí svou vlastní výzvu `> `. Server loguje obě připojení. Hráči jsou vzájemně nezávislí.

---

### TC-MVP-04 — Přihlášení neexistujícího uživatele (registrace)

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-04 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – přihlášení / registrace |
| **Předpoklady** | Server běží, účet `new_player_001` neexistuje |

**Kroky:**
1. Připoj klienta.
2. Na výzvu `Zadej své jméno:` zadej `new_player_001`.
3. Na výzvu `Nový hráč! Zvol heslo:` zadej `New123`.

**Očekávaný výsledek:**  
Server zobrazí:
```
Vítej, new_player_001! Napiš 'pomoc' pro seznam příkazů.
```
A ihned zobrazí popis místnosti `Vstupní síň`.

---

### TC-MVP-05 — Přihlášení existujícího uživatele — špatné heslo

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-05 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – přihlášení |
| **Předpoklady** | Účet `test_player` s heslem `Test123` existuje |

**Kroky:**
1. Připoj klienta.
2. Na výzvu `Zadej své jméno:` zadej `test_player`.
3. Na výzvu `Zadej heslo:` zadej `WrongPass`.

**Očekávaný výsledek:**  
Server zobrazí `Špatné heslo. Odpojuji.` a ukončí spojení. Klient zobrazí `Odpojeno od serveru.`

---

### TC-MVP-06 — Přihlášení existujícího uživatele — správné heslo

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-06 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – přihlášení |
| **Předpoklady** | Účet `test_player` s heslem `Test123` existuje |

**Kroky:**
1. Připoj klienta.
2. Na výzvu `Zadej své jméno:` zadej `test_player`.
3. Na výzvu `Zadej heslo:` zadej `Test123`.

**Očekávaný výsledek:**  
Server zobrazí:
```
Vítej, test_player! Napiš 'pomoc' pro seznam příkazů.

═══ Vstupní síň ═══
Vlhká kamenná síň s pochodněmi na zdech...
Východy:  sever, vychod
Předměty: svíčka
NPC:      Starý strážce
Hráči:    nikdo další
```

---

### TC-MVP-07 — Pohyb mezi místnostmi

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-07 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – pohyb |
| **Předpoklady** | Přihlášen jako `test_player` ve `Vstupní síni` |

**Kroky:**
1. Zadej příkaz `jdi sever`.

**Očekávaný výsledek:**  
Server zobrazí popis místnosti `Chodba kostí`:
```
═══ Chodba kostí ═══
Úzká chodba lemovaná hromadami starých kostí...
Východy:  jih, vychod
Předměty: nic
NPC:      Goblin hlídač
Hráči:    nikdo další
```

---

### TC-MVP-08 — Pohyb neplatným směrem

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-08 |
| **Priorita** | Střední |
| **Oblast** | MVP – pohyb (chybný vstup) |
| **Předpoklady** | Přihlášen jako `test_player` ve `Vstupní síni` |

**Kroky:**
1. Zadej příkaz `jdi zapad`.

**Očekávaný výsledek:**  
Server zobrazí: `⚠ Směr 'zapad' odtud nevede nikam.`  
Hráč zůstane ve `Vstupní síni`.

---

### TC-MVP-09 — Zobrazení místnosti (prozkoumej)

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-09 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – prozkoumej |
| **Předpoklady** | Přihlášen jako `test_player` ve `Vstupní síni` |

**Kroky:**
1. Zadej příkaz `prozkoumej`.

**Očekávaný výsledek:**  
Server zobrazí název místnosti, popis, seznam východů, předměty v místnosti a NPC.

---

### TC-MVP-10 — Sebrání předmětu

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-10 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – předměty |
| **Předpoklady** | Přihlášen jako `test_player` ve `Vstupní síni`, `svíčka` leží v místnosti |

**Kroky:**
1. Zadej příkaz `vezmi svíčka`.

**Očekávaný výsledek:**  
Server zobrazí: `Vzal jsi: svíčka.` následované popisem předmětu. Po příkazu `prozkoumej` se `svíčka` nezobrazí v Předměty.

---

### TC-MVP-11 — Sebrání neexistujícího předmětu

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-11 |
| **Priorita** | Střední |
| **Oblast** | MVP – předměty (chybný vstup) |
| **Předpoklady** | Přihlášen jako `test_player` ve `Vstupní síni` |

**Kroky:**
1. Zadej příkaz `vezmi zlatý pohár`.

**Očekávaný výsledek:**  
Server zobrazí: `Předmět 'zlatý pohár' tu není.`

---

### TC-MVP-12 — Odložení předmětu

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-12 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – předměty |
| **Předpoklady** | `test_player` má v inventáři `svíčka` (po TC-MVP-10) |

**Kroky:**
1. Zadej příkaz `odloz svíčka`.

**Očekávaný výsledek:**  
Server zobrazí: `Odložil jsi: svíčka.` Po příkazu `prozkoumej` se `svíčka` zobrazí v Předměty místnosti.

---

### TC-MVP-13 — Inventář

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-13 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – inventář |
| **Předpoklady** | Přihlášen jako `test_player` s alespoň 1 předmětem v inventáři |

**Kroky:**
1. Zadej příkaz `inventar`.

**Očekávaný výsledek:**  
Server zobrazí seznam předmětů a kapacitu, například:
```
── Inventář ──────────────────────────────
  • svíčka
Kapacita: 1/10
──────────────────────────────────────────
```

---

### TC-MVP-14 — Rozhovor s NPC

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-14 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – NPC |
| **Předpoklady** | `test_player` ve `Vstupní síni` (kde je `Starý strážce`) |

**Kroky:**
1. Zadej příkaz `mluv Starý strážce`.

**Očekávaný výsledek:**  
Server zobrazí: `Starý strážce: "Zadrž, poutníče! Toto místo skrývá mnohá nebezpečí..."`

---

### TC-MVP-15 — Rozhovor s neexistujícím NPC

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-15 |
| **Priorita** | Střední |
| **Oblast** | MVP – NPC (chybný vstup) |
| **Předpoklady** | `test_player` ve `Vstupní síni` |

**Kroky:**
1. Zadej příkaz `mluv Čaroděj`.

**Očekávaný výsledek:**  
Server zobrazí: `Žádné NPC jménem 'Čaroděj' tu není.`

---

### TC-MVP-16 — Zobrazení ostatních hráčů v místnosti

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-16 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – více hráčů |
| **Předpoklady** | `test_player` a `player_two` jsou oba ve `Vstupní síni` |

**Kroky:**
1. Přihlas `test_player` (terminál A).
2. Přihlas `player_two` (terminál B), oba ve `Vstupní síni`.
3. V terminálu A zadej `prozkoumej`.

**Očekávaný výsledek:**  
Výpis obsahuje: `Hráči:    player_two`

---

### TC-MVP-17 — Příkaz pomoc

| Pole | Hodnota |
|---|---|
| **ID** | TC-MVP-17 |
| **Priorita** | Vysoká |
| **Oblast** | MVP – nápověda |
| **Předpoklady** | Přihlášen jako `test_player` |

**Kroky:**
1. Zadej příkaz `pomoc`.

**Očekávaný výsledek:**  
Server zobrazí tabulku příkazů obsahující alespoň: `prozkoumej`, `jdi`, `vezmi`, `odloz`, `inventar`, `mluv`, `utok`, `pouzij`, `rekni`, `krik`, `zebricek`, `pomoc`.

---

## Povinné požadavky I1–I4 a P1 (min. 5 testů)

### TC-I1-01 — Načítání herního světa z externích souborů

| Pole | Hodnota |
|---|---|
| **ID** | TC-I1-01 |
| **Priorita** | Vysoká |
| **Oblast** | I1 – externí soubory |
| **Předpoklady** | Soubory `data/rooms.json`, `data/items.json`, `data/npcs.json` existují |

**Kroky:**
1. Spusť server (`dotnet run`).
2. Připoj klienta a přihlas se jako `test_player`.
3. Zadej `prozkoumej`.

**Očekávaný výsledek:**  
Místnost `Vstupní síň` je zobrazena se správným popisem a NPC `Starý strážce` — data načtena ze souborů JSON.

---

### TC-I1-02 — Chybějící datový soubor zablokuje start serveru

| Pole | Hodnota |
|---|---|
| **ID** | TC-I1-02 |
| **Priorita** | Střední |
| **Oblast** | I1 – externí soubory (chybný vstup) |
| **Předpoklady** | Soubor `data/rooms.json` přejmenován na `rooms.json.bak` |

**Kroky:**
1. Přejmenuj `data/rooms.json` na `data/rooms.json.bak`.
2. Pokus se spustit server (`dotnet run`).

**Očekávaný výsledek:**  
Server vypíše chybovou zprávu (výjimka o nenalezeném souboru) a nespustí se. Obnov soubor po testu.

---

### TC-I2-01 — Logování událostí

| Pole | Hodnota |
|---|---|
| **ID** | TC-I2-01 |
| **Priorita** | Střední |
| **Oblast** | I2 – logování |
| **Předpoklady** | Server běží, soubor `log.txt` existuje nebo bude vytvořen |

**Kroky:**
1. Spusť server.
2. Připoj klienta a přihlas se jako `test_player` s heslem `Test123`.
3. Zadej příkaz `jdi sever`.
4. Odpoj klienta (Ctrl+C).
5. Otevři soubor `log.txt`.

**Očekávaný výsledek:**  
`log.txt` obsahuje záznamy s časovými razítky a úrovní `[INFO]` pro: spuštění serveru, přihlášení hráče `test_player`, příkaz `jdi sever` a odpojení hráče `test_player`.

---

### TC-I3-01 — Persistence hráče — ukládání a načítání stavu

| Pole | Hodnota |
|---|---|
| **ID** | TC-I3-01 |
| **Priorita** | Vysoká |
| **Oblast** | I3 – persistence hráče |
| **Předpoklady** | Účet `test_player` existuje |

**Kroky:**
1. Přihlas se jako `test_player` / `Test123`.
2. Zadej `jdi sever` (přejdi do `Chodba kostí`).
3. Vezmi případný předmět nebo si zapamatuj aktuální místnost.
4. Odpoj klienta (Ctrl+C).
5. Znovu připoj klienta a přihlas se jako `test_player` / `Test123`.

**Očekávaný výsledek:**  
Hráč se přihlásí do místnosti `Chodba kostí` (uložená pozice), inventář je zachován.

---

### TC-I4-01 — Vlastní klientský program

| Pole | Hodnota |
|---|---|
| **ID** | TC-I4-01 |
| **Priorita** | Vysoká |
| **Oblast** | I4 – vlastní klient |
| **Předpoklady** | Server běží |

**Kroky:**
1. Ve složce `MudClient/` spusť `dotnet run -- 127.0.0.1 4000`.

**Očekávaný výsledek:**  
Klient se spustí jako samostatná konzolová aplikace, zobrazí banner, připojí se na zadanou IP a port, a umožní hráči zadávat příkazy. Nekomunikuje přes telnet ani netcat.

---

### TC-P1-01 — Dokončení hry — poražení finálního bosse

| Pole | Hodnota |
|---|---|
| **ID** | TC-P1-01 |
| **Priorita** | Vysoká |
| **Oblast** | P1 – dokončení hry |
| **Předpoklady** | `test_player` má v inventáři `rezavý meč` a `fragment zbroje`, je v místnosti `Drakův sál` |

**Kroky:**
1. Přihlas se jako `test_player` s dostatečným vybavením.
2. Dostaň se do místnosti `Drakův sál` (viz herní průchod níže).
3. Opakovaně zadávej `utok Drak Záhuby` dokud drak nezemře.

**Očekávaný výsledek:**  
Server zobrazí vítěznou obrazovku:
```
🏆═══════════════════════════════════════🏆
   GRATULUJEME, test_player!
   Porazil jsi Draka Záhuby a zachránil
   Zapomenuté podzemí!
   Čas: Xm Xs
🏆═══════════════════════════════════════🏆
```
Všichni ostatní připojení hráči obdrží zprávu `[SERVER] 🏆 test_player dokončil hru za Xm Xs!`.

---

### TC-P1-02 — Opakované přihlášení po dokončení hry

| Pole | Hodnota |
|---|---|
| **ID** | TC-P1-02 |
| **Priorita** | Střední |
| **Oblast** | P1 – dokončení hry |
| **Předpoklady** | `test_player` dokončil hru (TC-P1-01 prošel) |

**Kroky:**
1. Odpoj klienta.
2. Znovu přihlas `test_player` / `Test123`.

**Očekávaný výsledek:**  
Server zobrazí `[Již jsi dokončil hru. Gratuluji znovu!]` po přivítání.

---

## Herní mechaniky

### M — Zamčené místnosti a klíče

#### TC-M-LOCK-01 — Hráč bez klíče neprojde do zamčené místnosti

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-LOCK-01 |
| **Priorita** | Vysoká |
| **Oblast** | Zamčené místnosti |
| **Předpoklady** | `test_player` je ve `Zbrojnici`, nemá `starý klíč` v inventáři |

**Kroky:**
1. Zadej příkaz `jdi jih`.

**Očekávaný výsledek:**  
Server zobrazí: `⚠ Dveře jsou zamčené. Potřebuješ 'starý klíč'.`  
Hráč zůstane ve `Zbrojnici`.

---

#### TC-M-LOCK-02 — Hráč s klíčem projde do zamčené místnosti

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-LOCK-02 |
| **Priorita** | Vysoká |
| **Oblast** | Zamčené místnosti |
| **Předpoklady** | `test_player` je ve `Zbrojnici`, má `starý klíč` v inventáři |

**Kroky:**
1. Zadej příkaz `jdi jih`.

**Očekávaný výsledek:**  
Server zobrazí popis místnosti `Zapečetěná krypta`. Dveře jsou nyní odemčeny pro všechny hráče.

---

### M — Boj s NPC

#### TC-M-COMBAT-01 — Útok na neexistující NPC

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-COMBAT-01 |
| **Priorita** | Střední |
| **Oblast** | Boj |
| **Předpoklady** | `test_player` ve `Vstupní síni` |

**Kroky:**
1. Zadej příkaz `utok Drak`.

**Očekávaný výsledek:**  
Server zobrazí: `Žádné živé NPC 'Drak' tu není.`

---

#### TC-M-COMBAT-02 — Úspěšný útok na NPC (Goblin hlídač)

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-COMBAT-02 |
| **Priorita** | Vysoká |
| **Oblast** | Boj |
| **Předpoklady** | `test_player` v `Chodba kostí`, `Goblin hlídač` naživu |

**Kroky:**
1. Zadej příkaz `utok Goblin hlídač`.

**Očekávaný výsledek:**  
Server zobrazí způsobené poškození, HP nepřítele a HP hráče (po protiútoku). Pokud goblin zemře, server zobrazí `✓ Porazil jsi Goblin hlídač!` a loot `starý klíč` se objeví v místnosti.

---

#### TC-M-COMBAT-03 — Smrt hráče

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-COMBAT-03 |
| **Priorita** | Střední |
| **Oblast** | Boj — smrt hráče |
| **Předpoklady** | `test_player` má HP ≤ 5 (dosáhnout opakovaným bojem) |

**Kroky:**
1. Bojuj s Goblinem dokud HP neklesne na 0 nebo níže.

**Očekávaný výsledek:**  
Server zobrazí:
```
☠ Zemřel jsi! Probouzíš se zpět ve vstupní síni bez vybavení...
HP obnoveno na 50/100.
```
Inventář hráče je prázdný a hráč je ve `Vstupní síni`.

---

### M — Efekty (léčení, jed, buff)

#### TC-M-EFFECT-01 — Použití léčivé byliny

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-EFFECT-01 |
| **Priorita** | Vysoká |
| **Oblast** | Efekty – léčení |
| **Předpoklady** | `test_player` má HP < 100, má v inventáři `léčivá bylina` |

**Kroky:**
1. Zadej příkaz `pouzij léčivá bylina`.

**Očekávaný výsledek:**  
Server zobrazí: `💚 Použil jsi léčivá bylina. HP: X/100 (+30)`. HP se zvýší o 30 (max 100). Bylina zmizí z inventáře.

---

#### TC-M-EFFECT-02 — Použití jedovaté lahvičky na sebe

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-EFFECT-02 |
| **Priorita** | Střední |
| **Oblast** | Efekty – jed |
| **Předpoklady** | `test_player` má v inventáři `jedovatá lahvička` |

**Kroky:**
1. Zadej příkaz `pouzij jedovatá lahvička`.
2. Zadej libovolný příkaz (např. `prozkoumej`).

**Očekávaný výsledek:**  
Po použití server zobrazí `☠ Použil jsi jedovatá lahvička. Jed bude působit 4 tahů.`. Na začátku každého dalšího příkazu server zobrazí `☠ Jed tě poškozuje za 8 HP. (HP: X/100)`. Lahvička zmizí z inventáře.

---

#### TC-M-EFFECT-03 — Použití lektvaru síly

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-EFFECT-03 |
| **Priorita** | Střední |
| **Oblast** | Efekty – buff útoku |
| **Předpoklady** | `test_player` má v inventáři `lektvar síly` |

**Kroky:**
1. Zadej příkaz `stav` (zapamatuj si Útok).
2. Zadej příkaz `pouzij lektvar síly`.
3. Zadej příkaz `stav`.

**Očekávaný výsledek:**  
Po použití server zobrazí `⚔ Použil jsi lektvar síly. Útok +8 po 3 tahů.`. Hodnota Útok ve `stav` bude o 8 vyšší.

---

### M — Kapacita inventáře

#### TC-M-INV-01 — Plný inventář neumožní sebrat další předmět

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-INV-01 |
| **Priorita** | Střední |
| **Oblast** | Inventář – kapacita |
| **Předpoklady** | `test_player` má v inventáři 10 předmětů (max kapacita = 10) |

**Kroky:**
1. Jdi do místnosti s předmětem v místnosti.
2. Zadej `vezmi <předmět>`.

**Očekávaný výsledek:**  
Server zobrazí: `Inventář je plný!` Předmět zůstane v místnosti.

---

#### TC-M-INV-02 — Odložení předmětu uvolní místo

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-INV-02 |
| **Priorita** | Střední |
| **Oblast** | Inventář – kapacita |
| **Předpoklady** | `test_player` má plný inventář (10 předmětů), nachází se v místnosti s dalším předmětem |

**Kroky:**
1. Zadej `odloz svíčka` (nebo jiný předmět z inventáře).
2. Zadej `vezmi <předmět z místnosti>`.

**Očekávaný výsledek:**  
Krok 1: `Odložil jsi: svíčka.` — inventář nyní 9/10.  
Krok 2: `Vzal jsi: <předmět>.` — inventář opět 10/10.

---

### M — Chat mezi hráči (rekni / krik)

#### TC-M-CHAT-01 — Zpráva hráčům v místnosti (rekni) — prázdná místnost

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-CHAT-01 |
| **Priorita** | Nízká |
| **Oblast** | Chat – rekni |
| **Předpoklady** | `test_player` sám ve `Vstupní síni` |

**Kroky:**
1. Zadej příkaz `rekni Ahoj všichni!`.

**Očekávaný výsledek:**  
Server zobrazí: `Nikdo tě neslyší...` a `Říkáš: Ahoj všichni!`

---

#### TC-M-CHAT-02 — Zpráva hráčům v místnosti (rekni) — s dalším hráčem

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-CHAT-02 |
| **Priorita** | Střední |
| **Oblast** | Chat – rekni |
| **Předpoklady** | `test_player` a `player_two` jsou oba ve `Vstupní síni` |

**Kroky:**
1. V terminálu `test_player` zadej `rekni Ahoj player_two!`.

**Očekávaný výsledek:**  
- Terminál `test_player`: `Říkáš: Ahoj player_two!`  
- Terminál `player_two`: `[test_player říká]: Ahoj player_two!`

---

#### TC-M-CHAT-03 — Globální zpráva (krik)

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-CHAT-03 |
| **Priorita** | Střední |
| **Oblast** | Chat – krik |
| **Předpoklady** | `test_player` a `player_two` jsou připojeni, v různých místnostech |

**Kroky:**
1. Pošli `player_two` do `Chodba kostí` (příkaz `jdi sever`).
2. V terminálu `test_player` (ve `Vstupní síni`) zadej `krik Pozor, goblin!`.

**Očekávaný výsledek:**  
- Terminál `test_player`: `Křičíš: Pozor, goblin!`  
- Terminál `player_two`: `[KŘIK] test_player: Pozor, goblin!`

---

### M — Žebříček (leaderboard)

#### TC-M-LEADERBOARD-01 — Zobrazení prázdného žebříčku

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-LEADERBOARD-01 |
| **Priorita** | Nízká |
| **Oblast** | Žebříček |
| **Předpoklady** | Soubor `data/leaderboard.json` je prázdný (`[]`) |

**Kroky:**
1. Přihlas se jako `test_player`.
2. Zadej příkaz `zebricek`.

**Očekávaný výsledek:**  
Server zobrazí: `  Žebříček je zatím prázdný.`

---

#### TC-M-LEADERBOARD-02 — Záznam v žebříčku po dokončení hry

| Pole | Hodnota |
|---|---|
| **ID** | TC-M-LEADERBOARD-02 |
| **Priorita** | Střední |
| **Oblast** | Žebříček |
| **Předpoklady** | `test_player` právě dokončil hru (porazil Draka Záhuby) |

**Kroky:**
1. Zadej příkaz `zebricek`.

**Očekávaný výsledek:**  
Server zobrazí tabulku s řádkem obsahujícím `test_player` a čas dokončení.

---

## Herní průchod (quick reference pro testery)

```
Start: Vstupní síň
  → jdi vychod → Studna přání (vezmi léčivá bylina, rezavý meč)
  → jdi zapad  → Vstupní síň
  → jdi sever  → Chodba kostí
  → utok Goblin hlídač (opakuj)
  → [v místnosti se objeví: starý klíč] → vezmi starý klíč
  → jdi vychod → Zbrojnice (vezmi fragment zbroje, lektvar síly)
  → jdi jih    → Zapečetěná krypta [odemčena klíčem]
  → jdi vychod → Drakův sál
  → utok Drak Záhuby (opakuj, používej léčivou bylinu a lektvar síly)
  → [VÍTĚZSTVÍ]
```
