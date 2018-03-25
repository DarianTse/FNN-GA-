﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class Simulation : MonoBehaviour {

    public static Simulation Instance { get; private set; }

    //Lifeform prefab
    public GameObject lifeformPrefab;

    //Setup for population size and neural network size
    public int PopulationSize = 30;
    public uint[] NNLayersSize;

    //List of current lifeforms
    private List<LifeformController> lifeformControllers = new List<LifeformController>();
    private List<LifeForm> lifeforms = new List<LifeForm>();

    //Number of lifeforms alive
    public int LifeformsAliveCount { get; private set; }

    //The genetic algorithm
    private GeneticAlgorithm ga;

    //Event for all lifeforms dying
    public event Action AllLifeformsDead; 

    public uint GenerationCount { get { return ga.GenerationCount; } }  
    public int HighestFitness { get { return ga.HighestFitness; } }

    //Danger resources picked up
    public int DangerPicked { get; set; }
    //Energy resources picked up
    public int EnergyPicked { get; set; }

    void Awake()
    {
        if(Instance != null)
        {
            print("More than one manager exists");
            return;
        }
        Instance = this;
    }

    public void Start()
    {
        StartSimulation();
    }

    public void StartSimulation()
    {
        //Create neural network with proper Layer Sizes
        NeuralNetwork nn = new NeuralNetwork(NNLayersSize);

        //Setup genetic algorithm
        ga = new GeneticAlgorithm(nn.WeightCount, (uint)PopulationSize);
        ga.StartGeneration = InitialiseLifeforms;
        AllLifeformsDead += ga.EndGeneration;
        ga.Start();

#if UNITY_EDITOR
        lifeforms.ForEach(lifeform => print(lifeform.NN.GetNetworkString()));
#endif
    }

    //Create new lifeforms using the genotype population from the genetic algorithm
    private void InitialiseLifeforms(List<Genotype> currentPopulation)
    {
        lifeforms.Clear();
        LifeformsAliveCount = 0;
        DangerPicked = 0;
        EnergyPicked = 0;

        //Create list of lifeforms with genotypes in current population
        currentPopulation.ForEach(genotype => lifeforms.Add(new LifeForm(genotype, NNLayersSize)));

        //instantiate lifeforms first time, otherwise randomize position 
        if (lifeformControllers.Count < lifeforms.Count)
        {
            for (int i = 0; i < lifeforms.Count; i++)
            {
                GameObject newLifeform = Instantiate(lifeformPrefab, WorldController.Instance.GetRandomPosition(), Quaternion.identity);
                lifeformControllers.Add(newLifeform.GetComponent<LifeformController>());
            }
        }
        else
        {
            lifeformControllers.ForEach(lifeform => lifeform.transform.position = WorldController.Instance.GetRandomPosition());
        }

        //Link up lifeform controllers and lifeforms
        for (int i = 0; i < lifeforms.Count; i++)
        {
            lifeformControllers[i].GetComponent<LifeformController>().Lifeform = lifeforms[i];
            lifeformControllers[i].GetComponent<LifeformController>().Restart();
            LifeformsAliveCount++;
            lifeforms[i].LifeformDied += OnLifeformDied;
        }


    }

    //LifeformDied callback
    private void OnLifeformDied(LifeForm lifeform)
    {
        LifeformsAliveCount--;
        if(LifeformsAliveCount == 0 && AllLifeformsDead != null)
        {
            AllLifeformsDead();
        }
    }

}