import chalk from "chalk";
import chalkTemplate from "chalk-template";
import Config from "./Config.js";

export function IsNullOrEmpty(value: string | null | undefined): boolean {
    return !value || value.trim().length === 0;
}

export function Delay(seconds: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, seconds * 1000));
}

export class Console {
    public static Log(str: string) {
        console.log(chalkTemplate`{bold.greenBright [INFO]} ${ChalkedColorCodes(str)}`);
    }

    public static Warn(str: string) {
        console.log(chalkTemplate`{bold.orange [WARNING]} ${ChalkedColorCodes(str)}`);
    }

    public static Error(str: string) {
        console.log(chalkTemplate`{bold.red [ERROR]} ${ChalkedColorCodes(str)}`);
    }

    public static Debug(str: string) {
        if (!Config.Debug) return;
        console.log(chalkTemplate`{bold.yellow [DEBUG]} ${ChalkedColorCodes(str)}`);
    }
}

export class List<T> {
    private list: T[] = [];

    constructor(listData?: T[]) {
        if (listData)
            this.list = listData;
    }

    public Contains(item: T): boolean {
        return this.list.includes(item);
    }

    public Add(item: T) {
        this.list.push(item);
    }

    public AddMultiple(item: T[]) {
        this.list.push(...item);
    }

    public Insert(item: T, index: number) {
        this.list.splice(index, 0, item);
    }

    public Remove(item: T): boolean {
        const i = this.list.indexOf(item);
        if (i === -1) return false;
        this.list.splice(i, 1);
        return true;
    }

    public IndexOf(item: T): number {
        return this.list.indexOf(item);
    }

    public Get(i: number): T {
        if (i >= 0 && i < this.list.length) {
            return this.list[i];
        }
        throw new Error("Out of bounds in Array, Array size is " + this.list.length + ", requested is " + i);
    }

    public GetOrDefault(i: number): T | null {
        if (i >= 0 && i < this.list.length) {
            return this.list[i];
        }
        return null;
    }

    public Clear() {
        this.list = [];
    }

    public Count(): number {
        return this.list.length;
    }

    public Empty(): boolean {
        return this.list.length === 0;
    }

    public First(): T {
        return this.list[0];
    }

    public FirstOrDefault(): T | null {
        if (this.list.length < 1) return null;
        return this.list[0];
    }

    public Last(): T {
        return this.list[this.list.length - 1];
    }

    public LastOrDefault(): T | null {
        if (this.list.length < 1) return null;
        return this.list[this.list.length - 1] ?? null;
    }

    public Exists(predicate: (value: T, index: number, array: T[]) => unknown): boolean {
        return this.list.some(predicate);
    }

    public ToArray(): T[] {
        return this.list;
    }
}

// chat gptd cuz i dont carw
export function ChalkedColorCodes(input: string): string {
    const regex = /[§&]([0-9a-grlomn])/gi;
    let result = "";
    let currentStyles: ((txt: string) => string)[] = [];
    let bgMode = false;
    let lastIndex = 0;

    const flush = (text: string) => {
        if (!text) return "";
        let styledText = text;
        for (const style of currentStyles) {
            styledText = style(styledText);
        }
        return styledText;
    };

    let match;
    while ((match = regex.exec(input)) !== null) {
        const code = match[1].toLowerCase();
        const plainText = input.slice(lastIndex, match.index);
        result += flush(plainText);

        if (code === "r") {
            currentStyles = [];
            bgMode = false;
        } else if (code === "g") {
            bgMode = true;
        } else {
            const styleFn = getStyleFunction(code, bgMode);
            if (styleFn) currentStyles.push(styleFn);
        }

        lastIndex = regex.lastIndex;
    }

    result += flush(input.slice(lastIndex));
    return result;
}

function getStyleFunction(code: string, bg: boolean): ((txt: string) => string) | undefined {
    const colorMap: Record<string, string> = {
        "0": "black",
        "1": "blue",
        "2": "green",
        "3": "cyan",
        "4": "red",
        "5": "magenta",
        "6": "yellow",
        "7": "white",
        "8": "gray",
        "9": "blueBright",
        a: "greenBright",
        b: "cyanBright",
        c: "redBright",
        d: "magentaBright",
        e: "yellowBright",
        f: "whiteBright",
    };

    if (code in colorMap) {
        const color = colorMap[code];
        // @ts-ignore
        return bg ? chalk[`bg${capitalize(color)}`] : chalk[color];
    }

    const styleMap: Record<string, (txt: string) => string> = {
        l: chalk.bold,
        m: chalk.strikethrough,
        n: chalk.underline,
        o: chalk.italic,
    };

    return styleMap[code];
}

function capitalize(str: string) {
    return str.charAt(0).toUpperCase() + str.slice(1);
}