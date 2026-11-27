# HELL GATE

Arena graybox 3D esplorabile in prima persona con Unity 6000.6.2f1 e Universal Render Pipeline (URP 17.6.0).

## Avvio

1. Installare Unity 6000.6.2f1 tramite Unity Hub.
2. Aggiungere questa cartella come progetto e aprirla.
3. Attendere il ripristino dei pacchetti.
4. Aprire `Assets/Scenes/Graybox.unity` e premere Play.

La scena contiene stanze collegate, coperture, una rampa e una piattaforma rialzata, con materiali distinti e collisioni. Il giocatore dispone di visuale in prima persona, movimento, corsa e salto. Non sono ancora presenti armi o nemici.

## Comandi

- WASD: movimento.
- Mouse: visuale.
- Shift: corsa.
- Spazio: salto.
- Esc: libera il cursore; clic sinistro: riprende il controllo.

## Verifiche

Con la sola scena Graybox aperta, scegliere `HELLGATE > Validate Graybox` per controllare geometria, collisioni e riferimenti. I controlli batch `Hellgate.Editor.PlayerValidation.RunBatch` verificano movimento, corsa, salto, collisioni e attraversamento della rampa. `Hellgate.Editor.PlayerLifecycleValidation.Run` verifica due cicli Play/Stop e i binding della tastiera con input simulato. Quest'ultimo va avviato in batch senza `-quit` e termina autonomamente. L'input fisico e il cursore del sistema richiedono una verifica interattiva.

Sono disponibili soltanto i sorgenti; non è incluso un eseguibile.

Versionare gli asset insieme ai relativi file `.meta`. Cache, log e build generate sono esclusi dal repository.
