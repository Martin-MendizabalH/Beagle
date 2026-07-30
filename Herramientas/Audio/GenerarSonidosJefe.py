"""Genera la biblioteca procedural de sonidos del Jefe Tanque.

Los efectos combinan síntesis mecánica grave, señales electrónicas y una
cuantización suave de 12 bits para acompañar el pixel art sin sacrificar
compatibilidad: WAV PCM, mono, 44.1 kHz y 16 bits.
"""

from __future__ import annotations

import math
import random
import struct
import wave
from pathlib import Path


TASA_MUESTREO = 44_100
RAIZ_PROYECTO = Path(__file__).resolve().parents[2]
CARPETA_SALIDA = RAIZ_PROYECTO / "Assets" / "Audio" / "Jefes" / "Tanque"
SEMILLA_BASE = 73_041


def crear_buffer(duracion: float) -> list[float]:
    return [0.0] * max(1, round(duracion * TASA_MUESTREO))


def limitar(valor: float, minimo: float = -1.0, maximo: float = 1.0) -> float:
    return max(minimo, min(maximo, valor))


def suavizar(valor: float) -> float:
    valor = limitar(valor, 0.0, 1.0)
    return valor * valor * (3.0 - 2.0 * valor)


def envolvente(
    tiempo: float,
    duracion: float,
    ataque: float = 0.008,
    salida: float = 0.04,
    caida: float = 0.0,
) -> float:
    entrada = suavizar(tiempo / max(ataque, 1e-6))
    final = suavizar((duracion - tiempo) / max(salida, 1e-6))
    decaimiento = math.exp(-caida * tiempo) if caida > 0.0 else 1.0
    return entrada * final * decaimiento


def onda(fase: float, forma: str) -> float:
    ciclo = fase - math.floor(fase)
    if forma == "triangulo":
        return 1.0 - 4.0 * abs(ciclo - 0.5)
    if forma == "sierra":
        return 2.0 * ciclo - 1.0
    if forma == "cuadrada":
        return 1.0 if ciclo < 0.5 else -1.0
    return math.sin(2.0 * math.pi * ciclo)


def sumar_tono(
    datos: list[float],
    inicio: float,
    duracion: float,
    frecuencia_inicial: float,
    frecuencia_final: float | None = None,
    amplitud: float = 0.4,
    forma: str = "seno",
    ataque: float = 0.008,
    salida: float = 0.04,
    caida: float = 0.0,
    modulacion_frecuencia: float = 0.0,
    profundidad_modulacion: float = 0.0,
) -> None:
    comienzo = max(0, round(inicio * TASA_MUESTREO))
    cantidad = min(round(duracion * TASA_MUESTREO), len(datos) - comienzo)
    if cantidad <= 0:
        return

    frecuencia_final = (
        frecuencia_inicial if frecuencia_final is None else frecuencia_final
    )
    fase = 0.0
    for indice_local in range(cantidad):
        tiempo = indice_local / TASA_MUESTREO
        progreso = indice_local / max(1, cantidad - 1)
        frecuencia = frecuencia_inicial + (
            frecuencia_final - frecuencia_inicial
        ) * progreso
        if modulacion_frecuencia > 0.0:
            frecuencia *= 1.0 + profundidad_modulacion * math.sin(
                2.0 * math.pi * modulacion_frecuencia * tiempo
            )
        fase += frecuencia / TASA_MUESTREO
        datos[comienzo + indice_local] += (
            onda(fase, forma)
            * amplitud
            * envolvente(tiempo, duracion, ataque, salida, caida)
        )


def sumar_ruido(
    datos: list[float],
    inicio: float,
    duracion: float,
    amplitud: float,
    semilla: int,
    caida: float = 0.0,
    corte_bajo: float = 4_000.0,
    corte_alto: float = 0.0,
    ataque: float = 0.002,
    salida: float = 0.04,
) -> None:
    comienzo = max(0, round(inicio * TASA_MUESTREO))
    cantidad = min(round(duracion * TASA_MUESTREO), len(datos) - comienzo)
    if cantidad <= 0:
        return

    aleatorio = random.Random(SEMILLA_BASE + semilla)
    alfa_bajo = 1.0 - math.exp(
        -2.0 * math.pi * corte_bajo / TASA_MUESTREO
    )
    alfa_alto = (
        1.0 - math.exp(-2.0 * math.pi * corte_alto / TASA_MUESTREO)
        if corte_alto > 0.0
        else 0.0
    )
    filtro_bajo = 0.0
    filtro_alto = 0.0

    for indice_local in range(cantidad):
        tiempo = indice_local / TASA_MUESTREO
        blanco = aleatorio.uniform(-1.0, 1.0)
        filtro_bajo += alfa_bajo * (blanco - filtro_bajo)
        muestra = filtro_bajo
        if alfa_alto > 0.0:
            filtro_alto += alfa_alto * (blanco - filtro_alto)
            muestra = filtro_alto - filtro_bajo
        datos[comienzo + indice_local] += (
            muestra
            * amplitud
            * envolvente(tiempo, duracion, ataque, salida, caida)
        )


def sumar_ruido_periodico(
    datos: list[float],
    amplitud: float,
    semilla: int,
    frecuencia_minima: int,
    frecuencia_maxima: int,
    parciales: int = 18,
) -> None:
    """Ruido tonal periódico, sin discontinuidad al repetir un clip de 1 s."""
    aleatorio = random.Random(SEMILLA_BASE + semilla)
    componentes: list[tuple[int, float, float]] = []
    for _ in range(parciales):
        frecuencia = aleatorio.randint(frecuencia_minima, frecuencia_maxima)
        fase = aleatorio.random()
        peso = aleatorio.uniform(0.45, 1.0)
        componentes.append((frecuencia, fase, peso))

    normalizador = sum(componente[2] for componente in componentes)
    for indice in range(len(datos)):
        tiempo = indice / TASA_MUESTREO
        valor = 0.0
        for frecuencia, fase, peso in componentes:
            valor += math.sin(
                2.0 * math.pi * (frecuencia * tiempo + fase)
            ) * peso
        datos[indice] += amplitud * valor / normalizador


def sumar_golpe(
    datos: list[float],
    inicio: float,
    duracion: float,
    frecuencia: float,
    amplitud: float,
    semilla: int,
    metalico: bool = False,
) -> None:
    sumar_tono(
        datos,
        inicio,
        duracion,
        frecuencia * 1.9,
        frecuencia * 0.62,
        amplitud,
        "seno",
        0.001,
        0.035,
        6.5,
    )
    sumar_ruido(
        datos,
        inicio,
        duracion * 0.72,
        amplitud * 0.65,
        semilla,
        8.0,
        3_600.0 if metalico else 1_300.0,
        350.0 if metalico else 0.0,
        0.001,
        0.025,
    )
    if metalico:
        sumar_tono(
            datos,
            inicio,
            duracion,
            frecuencia * 5.2,
            frecuencia * 3.4,
            amplitud * 0.28,
            "triangulo",
            0.001,
            0.06,
            4.5,
        )


def preparar_salida(
    datos: list[float],
    bucle: bool,
    intensidad: float = 1.35,
    pico_objetivo: float = 0.88,
) -> list[int]:
    if not datos:
        return []

    media = sum(datos) / len(datos)
    datos = [muestra - media for muestra in datos]

    if not bucle:
        muestras_entrada = min(len(datos), round(0.004 * TASA_MUESTREO))
        muestras_salida = min(len(datos), round(0.028 * TASA_MUESTREO))
        for indice in range(muestras_entrada):
            datos[indice] *= suavizar(indice / max(1, muestras_entrada - 1))
        for indice in range(muestras_salida):
            posicion = len(datos) - 1 - indice
            datos[posicion] *= suavizar(indice / max(1, muestras_salida - 1))

    divisor = math.tanh(intensidad)
    datos = [math.tanh(muestra * intensidad) / divisor for muestra in datos]
    pico = max(abs(muestra) for muestra in datos) or 1.0
    escala = limitar(pico_objetivo, 0.05, 0.95) / pico

    # Cuantización interna de 12 bits: textura retro sutil dentro de PCM 16 bit.
    niveles = 2_047.0
    resultado: list[int] = []
    for muestra in datos:
        cuantizada = round(limitar(muestra * escala) * niveles) / niveles
        resultado.append(round(cuantizada * 32_767.0))
    return resultado


def guardar(
    nombre: str,
    datos: list[float],
    bucle: bool = False,
    pico_objetivo: float = 0.88,
) -> dict[str, float]:
    CARPETA_SALIDA.mkdir(parents=True, exist_ok=True)
    muestras = preparar_salida(datos, bucle, pico_objetivo=pico_objetivo)
    ruta = CARPETA_SALIDA / nombre
    with wave.open(str(ruta), "wb") as archivo:
        archivo.setnchannels(1)
        archivo.setsampwidth(2)
        archivo.setframerate(TASA_MUESTREO)
        archivo.writeframes(struct.pack("<" + "h" * len(muestras), *muestras))

    pico = max(abs(valor) for valor in muestras) / 32_767.0
    rms = math.sqrt(
        sum((valor / 32_767.0) ** 2 for valor in muestras) / len(muestras)
    )
    salto_bucle = (
        abs(muestras[-1] - muestras[0]) / 32_767.0 if bucle else 0.0
    )
    return {
        "duracion": len(muestras) / TASA_MUESTREO,
        "pico": pico,
        "rms": rms,
        "salto_bucle": salto_bucle,
    }


def generar_activacion() -> list[float]:
    datos = crear_buffer(0.95)
    sumar_golpe(datos, 0.0, 0.32, 72.0, 0.75, 1, True)
    for indice, (inicio, frecuencia) in enumerate(
        ((0.18, 210.0), (0.37, 315.0), (0.56, 470.0))
    ):
        sumar_tono(
            datos, inicio, 0.14, frecuencia, frecuencia * 1.08,
            0.33 + indice * 0.04, "cuadrada", 0.004, 0.035, 2.0,
        )
    sumar_tono(
        datos, 0.16, 0.72, 48.0, 92.0, 0.42, "sierra",
        0.04, 0.08, 0.7,
    )
    return datos


def generar_movimiento() -> list[float]:
    datos = crear_buffer(1.0)
    for indice in range(len(datos)):
        tiempo = indice / TASA_MUESTREO
        modulacion = 0.74 + 0.14 * math.sin(2.0 * math.pi * 8.0 * tiempo)
        motor = (
            0.34 * math.sin(2.0 * math.pi * 48.0 * tiempo)
            + 0.16 * onda(96.0 * tiempo, "triangulo")
            + 0.08 * onda(192.0 * tiempo, "triangulo")
        )
        # El golpe de la oruga ocurre a mitad del archivo, no en su empalme.
        fase_oruga = (tiempo * 8.0 + 0.5) % 1.0
        oruga = math.exp(-fase_oruga * 22.0) * (
            0.16 * math.sin(2.0 * math.pi * 310.0 * tiempo)
            + 0.12 * math.sin(2.0 * math.pi * 457.0 * tiempo)
        )
        datos[indice] = motor * modulacion + oruga
    sumar_ruido_periodico(datos, 0.16, 2, 35, 520, 22)
    return datos


def generar_anticipo_metralla() -> list[float]:
    datos = crear_buffer(0.78)
    for indice in range(5):
        inicio = 0.08 + indice * 0.105
        sumar_golpe(datos, inicio, 0.095, 115.0 + indice * 12.0, 0.28, 10 + indice, True)
    sumar_tono(
        datos, 0.05, 0.66, 95.0, 260.0, 0.35, "sierra",
        0.03, 0.06, 0.2, 12.0, 0.025,
    )
    return datos


def generar_disparo_metralla() -> list[float]:
    datos = crear_buffer(0.19)
    sumar_golpe(datos, 0.0, 0.18, 105.0, 0.95, 20, True)
    sumar_ruido(datos, 0.0, 0.11, 0.62, 21, 17.0, 6_000.0, 700.0, 0.001, 0.02)
    return datos


def generar_impacto_metralla() -> list[float]:
    datos = crear_buffer(0.24)
    sumar_golpe(datos, 0.0, 0.23, 68.0, 0.82, 30, True)
    sumar_tono(datos, 0.01, 0.2, 380.0, 250.0, 0.24, "triangulo", 0.001, 0.06, 5.0)
    return datos


def generar_anticipo_laser() -> list[float]:
    datos = crear_buffer(1.08)
    sumar_tono(
        datos, 0.0, 1.04, 72.0, 620.0, 0.38, "sierra",
        0.035, 0.035, 0.0, 18.0, 0.028,
    )
    sumar_tono(
        datos, 0.14, 0.88, 145.0, 940.0, 0.27, "seno",
        0.02, 0.04, 0.0, 25.0, 0.018,
    )
    for indice in range(7):
        inicio = 0.18 + indice * 0.115
        frecuencia = 310.0 + indice * 82.0
        sumar_tono(
            datos, inicio, 0.055, frecuencia, frecuencia * 1.12,
            0.22 + indice * 0.018, "cuadrada", 0.002, 0.018, 1.0,
        )
    return datos


def generar_laser() -> list[float]:
    datos = crear_buffer(1.0)
    for indice in range(len(datos)):
        tiempo = indice / TASA_MUESTREO
        pulso = 0.78 + 0.12 * math.sin(2.0 * math.pi * 7.0 * tiempo)
        zumbido = (
            0.28 * onda(90.0 * tiempo, "triangulo")
            + 0.22 * onda(180.0 * tiempo, "triangulo")
            + 0.16 * math.sin(2.0 * math.pi * 540.0 * tiempo)
            + 0.11 * math.sin(2.0 * math.pi * 1_080.0 * tiempo)
        )
        datos[indice] = zumbido * pulso
    sumar_ruido_periodico(datos, 0.27, 40, 250, 2_400, 28)
    return datos


def generar_fin_laser() -> list[float]:
    datos = crear_buffer(0.52)
    sumar_tono(datos, 0.0, 0.48, 680.0, 72.0, 0.58, "sierra", 0.002, 0.07, 2.8)
    sumar_ruido(datos, 0.05, 0.42, 0.38, 41, 5.0, 4_000.0, 450.0, 0.002, 0.07)
    return datos


def generar_anticipo_embestida() -> list[float]:
    datos = crear_buffer(0.76)
    sumar_tono(
        datos, 0.0, 0.73, 42.0, 118.0, 0.54, "sierra",
        0.025, 0.05, 0.1, 9.0, 0.04,
    )
    for indice in range(4):
        sumar_golpe(
            datos, 0.17 + indice * 0.13, 0.095,
            82.0 + indice * 14.0, 0.28, 50 + indice, True,
        )
    return datos


def generar_embestida() -> list[float]:
    datos = crear_buffer(1.0)
    for indice in range(len(datos)):
        tiempo = indice / TASA_MUESTREO
        vibracion = 0.82 + 0.1 * math.sin(2.0 * math.pi * 12.0 * tiempo)
        datos[indice] = vibracion * (
            0.36 * onda(76.0 * tiempo, "triangulo")
            + 0.22 * onda(152.0 * tiempo, "triangulo")
            + 0.12 * math.sin(2.0 * math.pi * 304.0 * tiempo)
        )
    sumar_ruido_periodico(datos, 0.2, 60, 40, 650, 24)
    return datos


def generar_impacto_pared() -> list[float]:
    datos = crear_buffer(0.68)
    sumar_golpe(datos, 0.0, 0.64, 48.0, 1.0, 70, True)
    sumar_tono(datos, 0.012, 0.6, 410.0, 175.0, 0.42, "triangulo", 0.001, 0.1, 3.3)
    sumar_tono(datos, 0.02, 0.55, 690.0, 330.0, 0.24, "seno", 0.001, 0.12, 4.0)
    sumar_ruido(datos, 0.04, 0.58, 0.58, 71, 4.5, 3_000.0, 180.0, 0.001, 0.08)
    return datos


def generar_anticipo_misil() -> list[float]:
    datos = crear_buffer(0.82)
    sumar_golpe(datos, 0.02, 0.25, 80.0, 0.45, 80, True)
    for indice, frecuencia in enumerate((420.0, 560.0, 760.0, 980.0)):
        inicio = 0.19 + indice * 0.135
        sumar_tono(
            datos, inicio, 0.085, frecuencia, frecuencia,
            0.36, "cuadrada", 0.003, 0.025, 1.2,
        )
    sumar_tono(datos, 0.14, 0.6, 55.0, 135.0, 0.3, "sierra", 0.03, 0.06, 0.3)
    return datos


def generar_lanzamiento_misil() -> list[float]:
    datos = crear_buffer(0.52)
    sumar_golpe(datos, 0.0, 0.28, 75.0, 0.78, 90, False)
    sumar_ruido(datos, 0.015, 0.47, 0.72, 91, 3.6, 5_500.0, 180.0, 0.001, 0.08)
    sumar_tono(datos, 0.035, 0.44, 155.0, 420.0, 0.3, "sierra", 0.005, 0.07, 1.2)
    return datos


def generar_explosion_misil() -> list[float]:
    datos = crear_buffer(0.82)
    sumar_golpe(datos, 0.0, 0.74, 43.0, 1.0, 100, False)
    sumar_ruido(datos, 0.0, 0.78, 0.92, 101, 4.8, 2_900.0, 0.0, 0.001, 0.1)
    for indice in range(4):
        sumar_golpe(
            datos, 0.13 + indice * 0.105, 0.22,
            105.0 + indice * 18.0, 0.22, 102 + indice, True,
        )
    return datos


def generar_dano() -> list[float]:
    datos = crear_buffer(0.24)
    sumar_golpe(datos, 0.0, 0.22, 125.0, 0.62, 110, True)
    sumar_tono(datos, 0.0, 0.2, 920.0, 360.0, 0.38, "triangulo", 0.001, 0.04, 7.0)
    return datos


def generar_transicion_fase() -> list[float]:
    datos = crear_buffer(1.48)
    sumar_tono(
        datos, 0.0, 1.42, 48.0, 185.0, 0.48, "sierra",
        0.04, 0.08, 0.0, 8.0, 0.04,
    )
    for indice in range(6):
        inicio = 0.12 + indice * 0.19
        frecuencia = 430.0 if indice % 2 == 0 else 620.0
        sumar_tono(
            datos, inicio, 0.12, frecuencia, frecuencia * 1.03,
            0.33, "cuadrada", 0.003, 0.025, 0.8,
        )
    sumar_golpe(datos, 1.12, 0.32, 78.0, 0.78, 120, True)
    sumar_tono(datos, 1.06, 0.34, 280.0, 920.0, 0.37, "sierra", 0.002, 0.06, 1.5)
    return datos


def generar_muerte() -> list[float]:
    datos = crear_buffer(1.9)
    sumar_golpe(datos, 0.0, 0.86, 38.0, 1.0, 130, False)
    sumar_ruido(datos, 0.0, 1.55, 0.84, 131, 2.7, 3_200.0, 0.0, 0.001, 0.14)
    sumar_golpe(datos, 0.32, 0.72, 53.0, 0.74, 132, True)
    sumar_golpe(datos, 0.68, 0.66, 46.0, 0.66, 133, True)
    sumar_tono(datos, 0.26, 1.55, 360.0, 34.0, 0.48, "sierra", 0.02, 0.16, 1.0)
    for indice in range(7):
        sumar_golpe(
            datos, 0.78 + indice * 0.115, 0.24,
            130.0 + indice * 24.0, 0.16, 140 + indice, True,
        )
    return datos


def main() -> None:
    generadores = [
        ("Jefe_Activacion.wav", generar_activacion, False, 0.72),
        ("Jefe_Movimiento_Bucle.wav", generar_movimiento, True, 0.55),
        ("Jefe_Metralla_Anticipo.wav", generar_anticipo_metralla, False, 0.60),
        ("Jefe_Metralla_Disparo.wav", generar_disparo_metralla, False, 0.38),
        ("Jefe_Metralla_Impacto.wav", generar_impacto_metralla, False, 0.45),
        ("Jefe_Laser_Anticipo.wav", generar_anticipo_laser, False, 0.65),
        ("Jefe_Laser_Bucle.wav", generar_laser, True, 0.62),
        ("Jefe_Laser_Final.wav", generar_fin_laser, False, 0.55),
        ("Jefe_Embestida_Anticipo.wav", generar_anticipo_embestida, False, 0.62),
        ("Jefe_Embestida_Bucle.wav", generar_embestida, True, 0.58),
        ("Jefe_Embestida_ImpactoPared.wav", generar_impacto_pared, False, 0.80),
        ("Jefe_Misil_Anticipo.wav", generar_anticipo_misil, False, 0.60),
        ("Jefe_Misil_Lanzamiento.wav", generar_lanzamiento_misil, False, 0.65),
        ("Jefe_Misil_Explosion.wav", generar_explosion_misil, False, 0.82),
        ("Jefe_Dano.wav", generar_dano, False, 0.58),
        ("Jefe_Fase2_Transicion.wav", generar_transicion_fase, False, 0.72),
        ("Jefe_Muerte.wav", generar_muerte, False, 0.85),
    ]

    print("Biblioteca de sonidos del Jefe Tanque")
    print(f"Salida: {CARPETA_SALIDA}")
    for nombre, generador, bucle, pico_objetivo in generadores:
        metricas = guardar(nombre, generador(), bucle, pico_objetivo)
        indicador_bucle = (
            f", salto={metricas['salto_bucle']:.4f}" if bucle else ""
        )
        print(
            f"- {nombre}: {metricas['duracion']:.2f}s, "
            f"pico={metricas['pico']:.3f}, rms={metricas['rms']:.3f}"
            f"{indicador_bucle}"
        )


if __name__ == "__main__":
    main()
