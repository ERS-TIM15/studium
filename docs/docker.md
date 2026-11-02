# Dockerfile uputstvo i objašnjenje
- Dockerfile predstavlja uputstvo docker build sistemu. Omogućava da se na svim mašinama reprodukuju isti koraci za build, test i run, unutar spremnih okruženja koji sadrže sve potrebne alate.

## Debug i Release
- U zavisnosti od toga da li je izabrana debug ili release opcija tokom izgradnje i testiranja menja se nekoliko stvari. Najznačajnija je ta da Debug opcija dozvoljava da se formira i pokrene finalni kontejner čak i ako neki od unit testova ne prođu (Za potrebe testiranja), dok Release opcija prekida izgradnju ukoliko neki od testova padne.

## Izgradnja i pokretanje kontejnera.

### Debug

- Izgradnja
```bash
sudo docker build --build-arg BUILD_CONFIGURATION=Debug -t studium:debug .
```

- Pokretanje u fg
```bash
sudo docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development studium:debug
```

- Pokretanje u bg
```bash
sudo docker run -d --name studium -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development studium:debug
```

### Release

- Izgradnja
```bash
sudo docker build -t studium:release .
```

- Pokretanje
```bash
sudo docker run -d --name studium -p 8080:8080 studium:release
```

