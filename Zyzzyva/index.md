Il componente Zyzzyva contiene un Dockerfile che permette eseguire la componente in modo singolo.

Per eseguire l'applicazione, è necessario l'utilizzo di docker. 
Per far partire il sistema con docker usando il file in questa radice basta eseguire il seguente comando nella radice della soluzione:
```bash
     docker build -t zyzzyvagrpc -f .\Zyzzyva\Dockerfile .
```
Dopo se si vuole eseguire bisogna eseguire 
```bash
    docker run -d -it -p portadestino:portainterna --hostname
```
 
