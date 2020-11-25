# Signal Beacon

![CodeQL](https://github.com/dfnoise/beacon/workflows/CodeQL/badge.svg)

## Services

- API
- Processor
- Zigbee2Mqtt
- Voice
- PhilipsHue
- Processor

### Voice

#### Wake word - Porcupine

Porcupine custom trained model for wake word `"signal"` expires every 30 days. You need to retrail one ourself or use provided profile if not expired. More info on training your custom wake work model can be found on [GitHub: Picovoice Porcupine repo](https://github.com/Picovoice/porcupine/).

#### DeepSpeech

To enable DeepSpeech support model and scorer needs to be added:

- `Profiles/deepspeech-x.x.x-models.pbmm`
- `Profiles/deepspeech-x.x.x-models.scorer`

Profiles can be downloaded from [Github: Mozilla DeepSpeech repo](https://github.com/mozilla/DeepSpeech) Releases page.
