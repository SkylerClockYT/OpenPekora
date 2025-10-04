import {Console, List} from "./CS.js";

// simple queue, requires port
export class Queue<T> {
    public pending: List<QueuePromise<T>> = new List<QueuePromise<T>>();
    public isProcessing = false;

    public port = 0;
    public boxId = "";

    constructor(boxId: string, port: number) {
        this.port = port;
        this.boxId = boxId;
    }

    public async Process() {
        if (this.isProcessing) return;
        this.isProcessing = true;
        Console.Debug(`Beginning to process queue on port ${this.port}`);

        while (!this.pending.Empty()) {
            const item = this.pending.First();
            this.pending.Remove(item);
            try {
                Console.Debug(`Processing item on port ${this.port}`);
                const time = new Date().getTime();
                const result = await item.task(this.port);
                const afterTime = new Date().getTime();
                item.resolve(result);
                Console.Debug(`Resolved item on port ${this.port}. Time to render: ${afterTime - time}ms`);
            } catch (e: any) {
                Console.Error(`Error while processing item on queue &c&l${this.port}&r in box &c&l${this.boxId}&r.\n &c&lMessage:&r \n ${e?.message} \n&c&lFull Error:&r \n ${e}\n`);
                item.reject(e);
            }
        }

        Console.Debug(`Done Processing items on port ${this.port}`);
        this.isProcessing = false;
    }

    public Add(item: QueuePromise<T>) {
        this.pending.Add(item);
    }
}

// queue box: does load balancing between queues
export class QueueBox<T> {
    public Queues = new List<Queue<T>>();
    public BoxId: string;

    constructor(boxId: string, ports: number[]) {
        this.BoxId = boxId;
        ports.forEach(port => this.Queues.Add(new Queue<T>(boxId, port)));
    }

    public async Enqueue(task: (port: number) => Promise<T>): Promise<T> {
        const queue = this.Queues.ToArray().reduce((shortest, current) => {
            return current.pending.Count() < shortest.pending.Count() ? current : shortest;
        });

        return new Promise<T>((res, rej) => {
            queue.Add({ task, resolve: res, reject: rej });
            Console.Debug(`Processing queue... Queue: ${queue.port}, Pending: ${queue.pending.Count()}`);
            queue.Process();
        });
    }
}

/**
 * due to us needing to insert selectedPort, we need to wrap the request promise into our own promise
 * (not 1 to 1 of how you"d use this but you get the idea)
 * export const RequestAvatarHeadshot = async (req, res) => {
 *     return await enqueueRender((selectedPort) =>
 *         handleRequest(req, res, HeadshotTemplate, 720, 720, selectedPort)
 *     );
 * };
 */

export interface QueuePromise<T> {
    task: (port: number) => Promise<T>;
    resolve: (value: T | PromiseLike<T>) => void;
    reject: (reason?: any) => void;
}
