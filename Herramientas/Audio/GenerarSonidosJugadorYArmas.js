const fs = require("fs");
const path = require("path");

const frecuenciaMuestreo = 44100;
const raiz = path.resolve(__dirname, "..", "..");

function aleatorioSemilla(semilla) {
    let estado = semilla >>> 0;
    return () => {
        estado = (1664525 * estado + 1013904223) >>> 0;
        return estado / 4294967296;
    };
}

function envolvente(t, duracion, ataque = 0.006, potencia = 2) {
    const entrada = Math.min(1, t / Math.max(0.0001, ataque));
    const salida = Math.pow(Math.max(0, 1 - t / duracion), potencia);
    return entrada * salida;
}

function limitarSuave(valor) {
    return Math.tanh(valor * 1.25) / Math.tanh(1.25);
}

function generar(duracion, semilla, sintetizador, picoObjetivo) {
    const cantidad = Math.ceil(duracion * frecuenciaMuestreo);
    const muestras = new Float64Array(cantidad);
    const azar = aleatorioSemilla(semilla);
    const estado = {};

    let pico = 0;
    let sumaCuadrados = 0;
    for (let i = 0; i < cantidad; i++) {
        const t = i / frecuenciaMuestreo;
        const valor = limitarSuave(sintetizador(t, duracion, azar, estado));
        muestras[i] = valor;
        pico = Math.max(pico, Math.abs(valor));
    }

    const escala = pico > 0 ? picoObjetivo / pico : 1;
    for (let i = 0; i < cantidad; i++) {
        muestras[i] *= escala;
        sumaCuadrados += muestras[i] * muestras[i];
    }

    return {
        muestras,
        pico: picoObjetivo,
        rms: Math.sqrt(sumaCuadrados / cantidad)
    };
}

function ruidoFiltrado(azar, estado, clave, suavizado) {
    const ruido = azar() * 2 - 1;
    const previo = estado[clave] || 0;
    const actual = previo + suavizado * (ruido - previo);
    estado[clave] = actual;
    return actual;
}

function escribirWav(rutaRelativa, resultado) {
    const ruta = path.join(raiz, rutaRelativa);
    fs.mkdirSync(path.dirname(ruta), { recursive: true });

    const datos = Buffer.alloc(resultado.muestras.length * 2);
    for (let i = 0; i < resultado.muestras.length; i++) {
        const muestra = Math.max(-1, Math.min(1, resultado.muestras[i]));
        datos.writeInt16LE(Math.round(muestra * 32767), i * 2);
    }

    const cabecera = Buffer.alloc(44);
    cabecera.write("RIFF", 0);
    cabecera.writeUInt32LE(36 + datos.length, 4);
    cabecera.write("WAVE", 8);
    cabecera.write("fmt ", 12);
    cabecera.writeUInt32LE(16, 16);
    cabecera.writeUInt16LE(1, 20);
    cabecera.writeUInt16LE(1, 22);
    cabecera.writeUInt32LE(frecuenciaMuestreo, 24);
    cabecera.writeUInt32LE(frecuenciaMuestreo * 2, 28);
    cabecera.writeUInt16LE(2, 32);
    cabecera.writeUInt16LE(16, 34);
    cabecera.write("data", 36);
    cabecera.writeUInt32LE(datos.length, 40);

    fs.writeFileSync(ruta, Buffer.concat([cabecera, datos]));
    console.log(
        `${rutaRelativa}: ${(resultado.muestras.length / frecuenciaMuestreo).toFixed(3)} s, ` +
        `pico ${resultado.pico.toFixed(2)}, RMS ${resultado.rms.toFixed(3)}`);
}

function paso(variante) {
    return generar(0.095, 1100 + variante, (t, d, azar, estado) => {
        const e = envolvente(t, d, 0.0025, 3.8);
        const grave = Math.sin(2 * Math.PI * (105 + variante * 13) * t) * Math.exp(-t * 34);
        const textura = ruidoFiltrado(azar, estado, "paso", 0.12) * Math.exp(-t * 42);
        const toque = Math.sin(2 * Math.PI * (360 + variante * 45) * t) * Math.exp(-t * 65);
        return e * (grave * 0.72 + textura * 1.1 + toque * 0.16);
    }, 0.43);
}

function salto() {
    return generar(0.19, 2201, (t, d, azar, estado) => {
        const progreso = t / d;
        const frecuencia = 270 + 430 * progreso * progreso;
        estado.fase = (estado.fase || 0) + 2 * Math.PI * frecuencia / frecuenciaMuestreo;
        const tono = Math.sin(estado.fase) + 0.22 * Math.sin(estado.fase * 2);
        const aire = ruidoFiltrado(azar, estado, "aire", 0.22) * (0.3 + 0.7 * progreso);
        return envolvente(t, d, 0.008, 1.7) * (tono * 0.78 + aire * 0.34);
    }, 0.56);
}

function dash() {
    return generar(0.235, 3301, (t, d, azar, estado) => {
        const progreso = t / d;
        const frecuencia = 235 - 125 * progreso;
        estado.fase = (estado.fase || 0) + 2 * Math.PI * frecuencia / frecuenciaMuestreo;
        const cuerpo = Math.sin(estado.fase) * Math.exp(-t * 8);
        const ruidoLento = ruidoFiltrado(azar, estado, "dashLento", 0.075);
        const ruidoRapido = ruidoFiltrado(azar, estado, "dashRapido", 0.34);
        const aire = (ruidoRapido - ruidoLento) * (1 - 0.3 * progreso);
        return envolvente(t, d, 0.006, 1.8) * (cuerpo * 0.5 + aire * 1.15);
    }, 0.61);
}

function pistola() {
    return generar(0.12, 4401, (t, d, azar, estado) => {
        const frecuencia = 205 * Math.exp(-t * 13) + 58;
        estado.fase = (estado.fase || 0) + 2 * Math.PI * frecuencia / frecuenciaMuestreo;
        const golpe = Math.sin(estado.fase) * Math.exp(-t * 31);
        const chasquido = ruidoFiltrado(azar, estado, "pistola", 0.42) * Math.exp(-t * 46);
        return envolvente(t, d, 0.0015, 3.2) * (golpe * 1.05 + chasquido * 1.45);
    }, 0.68);
}

function metralleta() {
    return generar(0.068, 5501, (t, d, azar, estado) => {
        const frecuencia = 265 * Math.exp(-t * 18) + 82;
        estado.fase = (estado.fase || 0) + 2 * Math.PI * frecuencia / frecuenciaMuestreo;
        const golpe = Math.sin(estado.fase) * Math.exp(-t * 48);
        const mecanismo = ruidoFiltrado(azar, estado, "mecanismo", 0.5) * Math.exp(-t * 60);
        return envolvente(t, d, 0.001, 3.8) * (golpe * 0.8 + mecanismo * 1.35);
    }, 0.58);
}

function escopeta() {
    return generar(0.27, 6601, (t, d, azar, estado) => {
        const frecuencia = 125 * Math.exp(-t * 7) + 42;
        estado.fase = (estado.fase || 0) + 2 * Math.PI * frecuencia / frecuenciaMuestreo;
        const golpe = Math.sin(estado.fase) * Math.exp(-t * 15);
        const ruido = ruidoFiltrado(azar, estado, "escopeta", 0.22);
        const cola = ruidoFiltrado(azar, estado, "cola", 0.045);
        return envolvente(t, d, 0.0018, 2.15) *
            (golpe * 1.3 + ruido * Math.exp(-t * 20) * 1.7 + cola * 0.55);
    }, 0.76);
}

function katana() {
    return generar(0.205, 7701, (t, d, azar, estado) => {
        const progreso = t / d;
        const base = ruidoFiltrado(azar, estado, "katanaBase", 0.055);
        const brillo = ruidoFiltrado(azar, estado, "katanaBrillo", 0.48) - base;
        const frecuencia = 520 + 780 * progreso;
        estado.fase = (estado.fase || 0) + 2 * Math.PI * frecuencia / frecuenciaMuestreo;
        const filo = Math.sin(estado.fase) * Math.sin(Math.PI * progreso);
        return envolvente(t, d, 0.012, 1.6) *
            (base * 0.75 + brillo * 0.82 + filo * 0.28);
    }, 0.64);
}

const sonidos = [
    ["Assets/Audio/Jugador/Jugador_Paso_1.wav", paso(1)],
    ["Assets/Audio/Jugador/Jugador_Paso_2.wav", paso(2)],
    ["Assets/Audio/Jugador/Jugador_Salto.wav", salto()],
    ["Assets/Audio/Jugador/Jugador_Dash.wav", dash()],
    ["Assets/Audio/Armas/Arma_Pistola.wav", pistola()],
    ["Assets/Audio/Armas/Arma_Metralleta.wav", metralleta()],
    ["Assets/Audio/Armas/Arma_Escopeta.wav", escopeta()],
    ["Assets/Audio/Armas/Arma_Katana.wav", katana()]
];

for (const [ruta, resultado] of sonidos) {
    escribirWav(ruta, resultado);
}
