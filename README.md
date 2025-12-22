# BootcampMatchValidation
Small project to fiddle around with C# and TFT/Twitch API

## Setup
- Install .NET 10 SDK.
- Copy `.env.example` to `.env` and fill in `RIOT_API_KEY`, `TWITCH_CLIENT_ID`, and `TWITCH_CLIENT_SECRET`.

## Build
```bash
dotnet build TftStreamChecker.sln
```

## Test
```bash
dotnet test TftStreamChecker.sln
```

## CLI flags (current)
- `--riotId "name#tag"` and `--twitch login` (single parse)
- `--input file` (batch JSON; top 30 by rank)
- `--concurrency N` (match detail fetch; default 1)
- `--outputCsv path` (empty to disable)
- `--noCache` (skip cache reads/writes)
- window flags: `--days`, `--startTime`, `--endTime`, `--eventYear`, `--eventStart`, `--eventEnd`

## Run
```bash
./run.sh --riotId "name#tag" --twitch login
```

## Usage examples
- Single participant:
  ```bash
  ./run.sh --riotId "Player#NA1" --twitch playerchannel --outputCsv output/stream-check.csv --concurrency 1
  ```
- BatchIG (place JSONs under `inputs/`):
  ```bash
  ./run.sh --input inputs/participants.json --outputCsv output/stream-check.csv --concurrency 1
  ```

## Defaults/toggles:
- `--concurrency` defaults to 1 (increase cautiously).
- `--outputCsv ""` disables CSV.
- `--noCache` skips `.cache` reads/writes.
- Riot 404 for Name Change catches and a zeroed CSV row.

## Batch input format
- Format is based off boxboxtft.com content served as so:
- JSON can be an array of participants like`{ "Participants": [ ... ] }`.
- Each participant uses fields like:
```json
{
  "Team": "pro",
  "Rank": 1,
  "Name": "DOGDOG",
  "Eliminated": false,
  "PotentiallyEliminated": false,
  "DayEliminated": 2,
  "Socials": [
    { "LinkUri": "https://twitch.tv/DOGDOG" }
  ],
  "RankUrl": "https://tft.op.gg/summoners/na/dogdogdog-na1"
}
```
- `RankUrl` is parsed for RiotId,
- `Socials[].LinkUri` for twitch login,
- `Eliminated`/`DayEliminated` clamps a window of time
