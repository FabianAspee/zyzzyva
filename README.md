#  Progetto Sistemi Distribuiti (SD)

Questo progetto per SD si propone di implementare l'algoritmo  Zyzzyva per i fallimenti bizantini. 

Per eseguire l'applicazione, è necessario l'utilizzo di docker. In particolare è stato preparato un file docker-compose.yml per permettere un rapido deploy di tutti i componenti necessari al funzionamento del sistema.
Per far partire il sistema con docker compose usando il file della release basta eseguire il seguente comando nella posizione in cui è presente il file docker-compose.yml:
```bash
    docker-compose up 
```
Utilizzando questo docker-compose, verranno scaricati da \textit{dockerhub} le immagini già pronte per essere utilizzate. 

Se invece si vuole eseguire usando direttamente il file contenuto nel repository,  si può utilizzare il comando:
```bash
    docker-compose up  --build
```
Utilizzando questo comando verranno creati e fatti partire i contenitori a partire dai sorgenti del progetto.

Se si vuole obbligare a ricostruire l'immagine ogni volta basta utilizzare il comando:
```bash
    docker-compose up --build --force-recreate
```
Per quanto riguarda la componente SMR di test, sono presenti due versioni dell'applicazione: una per sistemi windows ed una per sistemi linux ubuntu. Per eseguire l'applicazione è sufficiente estrarre il contenuto della versione desiderata che è nella release ed eseguire il file SMRViewZyzzva presente all'interno della cartella estratta. Per poter eseguire il componente SMRViewZyzzva direttamente dal progetto, è necessario avere installato dotnet ed eseguire, all'interno della cartella SMRViewZyzzva, il comando:
```bash
    dotnet run
```
L'ultimo step necessario è l'aggiunta del certificato pfx fornito all'elenco dei certificati trusted del proprio pc: per poter utilizzare grpc tramite https è infatti necessaria la presenza di un certificato ed abbiamo deciso di  crearne uno da utilizzare durante lo sviluppo ed il testing dell'applicazione. La password del certificato è test123 .

